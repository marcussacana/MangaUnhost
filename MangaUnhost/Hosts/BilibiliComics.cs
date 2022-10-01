using MangaUnhost.Browser;
using MangaUnhost.Others;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace MangaUnhost.Hosts
{
    class BilibiliComics : IHost
    {
        public NovelChapter DownloadChapter(int ID)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<byte[]> DownloadPages(int ID)
        {
            foreach (var Page in GetChapterPages(ID))
            {
                yield return Page.TryDownload(Referer: $"https://www.bilibilicomics.com/mc{ComicID}/{ID}?from=manga_detail");
            }
        }

        public ChapterInfo GetChapterInfo(int ID)
        {
            var jsonInfo = Post("https://www.bilibilicomics.com/twirp/comic.v1.Comic/GetImageIndex?mob_app=android_comic&version=2.14.0&device=android&platform=android&appkey=1d8b6e7d45233436&lang=en&sys_lang=en", $"{{\"ep_id\": {ID}}}");

            var RespInfo = Newtonsoft.Json.JsonConvert.DeserializeObject<ChapterInfoResponse>(jsonInfo);

            if (RespInfo.code != 0)
                throw new Exception(RespInfo.msg);

            return RespInfo.data;
        }

        public string[] GetChapterPages(int ID)
        {

            var Chapter = GetChapterInfo(ID);

            int Quality = Chapter.images.First().x;
            if (Quality >= 2000)
                Quality = 2000;
            else if (Quality >= 1600)
                Quality = 1600;
            else if (Quality >= 1200)
                Quality = 1200;
            else if (Quality >= 1000)
                Quality = 1000;

            List<string> Urls = new List<string>();
            foreach (var Image in Chapter.images)
            {
                string Path = $"{Image.path}@{Quality}w.webp";

                var Req = new GetTokenRequest() { urls = $"[\"{Path}\"]" };

                var jsonReq = Newtonsoft.Json.JsonConvert.SerializeObject(Req);

                var tokenResp = Post("https://www.bilibilicomics.com/twirp/comic.v1.Comic/ImageToken?mob_app=android_comic&version=2.14.0&device=android&platform=android&appkey=1d8b6e7d45233436&lang=en&sys_lang=en", jsonReq);

                var TokenInfo = Newtonsoft.Json.JsonConvert.DeserializeObject<GetTokenResponse>(tokenResp);
                if (TokenInfo.code != 0)
                    throw new Exception(TokenInfo.msg);

                var Token = TokenInfo.data.First();

                Urls.Add($"{Token.url}?token={Token.token}");
            }

            return Urls.ToArray();
        }

        public IEnumerable<KeyValuePair<int, string>> EnumChapters()
        {
            foreach (var Chap in Info.ep_list.Where(x => x.pay_gold == 0).OrderByDescending(x => x.ord))
            {
                yield return new KeyValuePair<int, string>(Chap.id, Chap.ord.ToString());
            }
        }

        public int GetChapterPageCount(int ID)
        {
            return GetChapterInfo(ID).images.Count;
        }

        public IDecoder GetDecoder()
        {
            return new Decoders.CommonImage();
        }

        public PluginInfo GetPluginInfo()
        {
            return new PluginInfo()
            {
                Name = "BiliBiliComics",
                Author = "Marcussacana",
                SupportComic = true,
                Version = new Version(1, 0)
            };
        }

        public bool IsValidPage(string HTML, Uri URL)
        {
            return false;
        }

        public bool IsValidUri(Uri Uri)
        {
            return Uri.AbsoluteUri.ToLowerInvariant().Contains("detail/mc") && Uri.Host.ToLowerInvariant().Contains("bilibilicomics.com");
        }

        public ComicInfo LoadUri(Uri Uri)
        {
            ComicID = Uri.AbsoluteUri.Substring("/mc").Split('?').First();
            var jsonInfo = Post("https://www.bilibilicomics.com/twirp/comic.v1.Comic/ComicDetail?device=android&lang=en&sys_lang=en", $"{{\"comic_id\":{ComicID}}}");

            var Response = Newtonsoft.Json.JsonConvert.DeserializeObject<ComicInfoResponse>(jsonInfo);
            if (Response.code != 0)
                throw new Exception(Response.msg);

            Info = Response.data;

            return new ComicInfo()
            {
                Title = Info.title,
                ContentType = ContentType.Comic,
                Cover = Info.vertical_cover.Download(Referer: "https://www.bilibilicomics.com/"),
                Url = Uri
            };
        }

        string ComicID;
        BiliBiliComicInfo Info;



        public string Post(string URI, string Data, string ContentType = "application/json;charset=UTF-8", string Referer = "https://www.bilibilicomics.com/")
        {
            var Request = HttpWebRequest.CreateHttp(URI);
            Request.Method = "POST";
            Request.ContentType = ContentType;
            Request.Referer = Referer;
            Request.UserAgent = ProxyTools.UserAgent;

            using (var DataStream = new MemoryStream(Encoding.UTF8.GetBytes(Data)))
            using (var SendStream = Request.GetRequestStream())
            {
                DataStream.CopyTo(SendStream);
            }

            var Response = Request.GetResponse();
            using (var ResponseData = new MemoryStream())
            using (var ResponseStream = Response.GetResponseStream())
            {
                ResponseStream.CopyTo(ResponseData);

                var FinalData = ResponseData.ToArray();
                return Encoding.UTF8.GetString(FinalData);
            }
        }
        struct ComicInfoResponse
        {
            public int code;
            public string msg;
            public BiliBiliComicInfo data;
        }

        struct BiliBiliComicInfo
        {
            public int id;
            public string title;
            public int comic_type;
            public int page_default;
            public int page_allow;
            public string horizontal_cover;
            public string square_cover;
            public string vertical_cover;
            public List<string> author_name;
            public List<string> styles;
            public int last_ord;
            public int is_finish;
            public int status;
            public string evaluate;
            public int total;
            public List<ComicEpisode> ep_list;

        }

        struct ComicEpisode
        {
            public int id;
            public int ord;
            public int read;
            public int play_mode;
            public bool is_locked;
            public int pay_gold;
            public long size;
            public string short_title;
            public bool is_in_free;
            public string title;
            public string cover;
            public string pub_time;
            public int comments;
            public bool allow_wait_free;
            public string progress;
            public int like_count;
            public int chapter_id;
            public int type;
            public int origin_gold;
            public bool is_risky;
        }
        public struct ChapterInfoResponse {
            public int code;
            public string msg;
            public ChapterInfo data;
        }

        public struct ChapterInfo {
            public string path;
            public List<ImageInfo> images;
        }

        public struct ImageInfo
        {
            public string path;
            public int x;
            public int y;
        }

        public struct GetTokenResponse
        {
            public int code;
            public string msg;
            public List<TokenData> data;
        }

        public struct TokenData
        {
            public string url;
            public string token;
        }

        public struct GetTokenRequest
        {
            public string urls;
        }
    }
}
