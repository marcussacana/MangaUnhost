using CefSharp;
using MangaUnhost.Browser;
using MangaUnhost.Decoders;
using MangaUnhost.Others;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MangaUnhost.Hosts
{
    internal class TsukiMangas : IHost
    {
        public NovelChapter DownloadChapter(int ID)
        {
            throw new NotImplementedException();
        }

        string[] Hosts = new string[] { "tsuki-mangas.com", "cdn2.tsuki-mangas.com", "cdn.tsuki-mangas.com" };
        public IEnumerable<byte[]> DownloadPages(int ID)
        {
            var Decoder = GetDecoder();
            int HostIndex = 1;
            string CurrentHost = Hosts.First();
            string CurrentDir = "/";
            foreach (var Page in GetChapterPages(ID))
            {
                byte[] Data = null;
                var PageURL = new Uri(new Uri($"https://{CurrentHost}"), $"{CurrentDir}{Page.TrimStart('/')}");
                try
                {
                    Data = PageURL.Download(UserAgent: ProxyTools.UserAgent, Referer: "https://tsuki-mangas.com/");
                    CheckImage(Decoder, Data);
                }
                catch
                {
                    for (int i = 0; i < Hosts.Length; i++)
                    {
                        CurrentHost = Hosts[HostIndex++ % Hosts.Length]; 
                        PageURL = new Uri(new Uri($"https://{CurrentHost}"), Page);
                        try
                        {
                            Data = PageURL.Download(UserAgent: ProxyTools.UserAgent, Referer: "https://tsuki-mangas.com/");
                            CheckImage(Decoder, Data);
                            CurrentDir = "/"; 
                            break;
                        }
                        catch { }

                        PageURL = new Uri(new Uri($"https://{CurrentHost}"), $"/tsuki/{Page.TrimStart('/')}");
                        try
                        {
                            Data = PageURL.Download(UserAgent: ProxyTools.UserAgent, Referer: "https://tsuki-mangas.com/");
                            CheckImage(Decoder, Data);
                            CurrentDir = "/tsuki/";
                            break;
                        }
                        catch { }
                    }
                }

                yield return Data;
            }
        }

        private static void CheckImage(IDecoder Decoder, byte[] Data)
        {
            if (!Main.IsWebP(Data))
            {
                using (var Img = Decoder.Decode(Data))
                {
                    if (Img == null)
                        throw new Exception();
                }
            }
        }

        private IEnumerable<string> GetChapterPages(int ID)
        {
            var JSON = new Uri($"https://tsuki-mangas.com/api/v2/chapter/versions/{ID}").TryDownloadString();
            var Info = Newtonsoft.Json.JsonConvert.DeserializeObject<ChapterDetails>(JSON);

            var Regex = new Regex("(\\d+)\\.(png|jpg|jpeg|gif|webp)", RegexOptions.IgnoreCase);
            try
            {
                return Info.pages.Select(x => x.url).OrderBy(x => int.Parse(Regex.Match(x).Groups[1].Value)).ToArray();
            }
            catch
            {
                return Info.pages.Select(x => x.url).ToArray();
            }
        }

        public IEnumerable<KeyValuePair<int, string>> EnumChapters()
        {
            ChaptersInfo info = new ChaptersInfo() { lastPage = 1 };
            for (int i = 1; i <= info.lastPage; i++)
            {
                var JSON = new Uri($"https://tsuki-mangas.com/api/v2/chapters?manga_id={MangaID}&order=desc&page={i}&filter=").TryDownloadString(UserAgent: ProxyTools.UserAgent);
                info = Newtonsoft.Json.JsonConvert.DeserializeObject<ChaptersInfo>(JSON);
                foreach (var Chapter in info.data)
                {
                    var ChapInfo = Chapter.versions.First();
                    yield return new KeyValuePair<int, string>(ChapInfo.id, Chapter.number);
                }
            }
        }

        public int GetChapterPageCount(int ID)
        {
            return GetChapterPages(ID).Count();
        }

        public IDecoder GetDecoder()
        {
            return new CommonImage();
        }

        public PluginInfo GetPluginInfo()
        {
            return new PluginInfo()
            {
                Name = "TsukiMangas",
                Author = "Marcussacana",
                SupportComic = true,
                Version = new Version(1, 0, 2)
            };
        }

        public bool IsValidPage(string HTML, Uri URL)
        {
            throw new NotImplementedException();
        }

        public bool IsValidUri(Uri Uri)
        {
            //https://tsuki-mangas.com/obra/582/sex-academy
            return Uri.PathAndQuery.Contains("/obra/") && !Uri.PathAndQuery.EndsWith("/obra/");
        }

        string MangaID = null;
        Uri CurrentUrl = null;
        public ComicInfo LoadUri(Uri Uri)
        {
            CurrentUrl = Uri;

            MangaID = Uri.PathAndQuery.Substring("/obra/", "/");

            var JSON = new Uri($"https://tsuki-mangas.com/api/v2/mangas/{MangaID}").TryDownloadString(UserAgent: ProxyTools.UserAgent);

            var Info = Newtonsoft.Json.JsonConvert.DeserializeObject<MangaInfo>(JSON);

            var Cover = new Uri($"https://tsuki-mangas.com/img/imgs/{Info.poster}").TryDownload(UserAgent: ProxyTools.UserAgent);

            return new ComicInfo()
            {
                Title = Info.title,
                Cover = Cover,
                Url = CurrentUrl,
                ContentType = ContentType.Comic
            };
        }


        public struct Title
        {
            public int id;
            public int manga_id;
            public string title;
            public DateTime created_at;
            public DateTime updated_at;
        }

        public struct Genre
        {
            public int id;
            public int manga_id;
            public string genre;
            public DateTime created_at;
            public DateTime updated_at;
        }

        public struct MangaInfo
        {
            public int id;
            public string url;
            public string title;
            public string status;
            public string synopsis;
            public string staff;
            public string poster;
            public string cover;
            public int format;
            public int adult_content;
            public string trailer;
            public int anilist_id;
            public double rating;
            public int total_rating;
            public int chapters_count;
            public int views;
            public int views_days;
            public int views_month;
            public DateTime last_published_at;
            public DateTime created_at;
            public DateTime updated_at;
            public string tags;
            public object golden;
            public object mc;
            public int isvisible;
            public List<Title> titles;
            public List<Genre> genres;
            public List<object> relationships;
        }
        public struct ChapterVersion
        {
            public int id;
            public int chapter_id;
            public int user_id;
            public int total_pages;
            public DateTime created_at;
            public DateTime updated_at;
            public int approved;
            public object user_approved;
            public List<object> scans;
        }

        public struct ChapterEntry
        {
            public int id;
            public int manga_id;
            public int user_id;
            public string number;
            public object title;
            public int approved;
            public int views;
            public DateTime created_at;
            public DateTime updated_at;
            public List<ChapterVersion> versions;
        }

        public struct ChaptersInfo
        {
            public int total;
            public int perPage;
            public int page;
            public int lastPage;
            public List<ChapterEntry> data;
        }
        public struct ChapterDetails
        {
            public int id;
            public int chapter_id;
            public int user_id;
            public int total_pages;
            public DateTime created_at;
            public DateTime updated_at;
            public int approved;
            public object user_approved;
            public ChapterEntry chapter;
            public List<object> scans;
            public List<PageInfo> pages;
        }
        public struct PageInfo
        {
            public int id;
            public int chapter_version_id;
            public string url;
            public int server;
            public DateTime created_at;
            public DateTime updated_at;
        }
    }
}
