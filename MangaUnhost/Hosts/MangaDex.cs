using MangaUnhost.Browser;
using MangaUnhost.Decoders;
using MangaUnhost.Others;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace MangaUnhost.Hosts
{
    class MangaDex : IHost
    {
        public NovelChapter DownloadChapter(int ID)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<byte[]> DownloadPages(int ID)
        {
            foreach (var Page in GetChapterPages(ID))
            {

                Dictionary<string, byte[]> Images = new Dictionary<string, byte[]>();
                Dictionary<string, int> MatchCount = new Dictionary<string, int>();

                while (true)
                {
                    var Image = Page.TryDownload(UserAgent: AccountTools.GenerateName(), Referer: "https://mangadex.org");
                    var Hash = GetHash(Image);
                    Images[Hash] = Image;

                    if (MatchCount.ContainsKey(Hash))
                        MatchCount[Hash]++;
                    else
                        MatchCount[Hash] = 1;

                    if (MatchCount.Count > 1)
                        ThreadTools.Wait(500);

                    bool OK = false;

                    foreach (var Pair in MatchCount)
                    {
                        if (Pair.Value > 3)
                        {
                            OK = true;
                            yield return Images[Pair.Key];
                            break;
                        }
                    }

                    if (OK)
                        break;
                }

                MatchCount.Clear();
                Images.Clear();
            }
        }

        public string GetHash(byte[] Data)
        {
            using (SHA256 Sha = SHA256.Create())
            {
                var Rst = Sha.ComputeHash(Data);
                return string.Join(" ", Rst.Select(x => x.ToString("X2")));
            }
        }

        Dictionary<int, string> ChapterInfo = new Dictionary<int, string>();
        public IEnumerable<KeyValuePair<int, string>> EnumChapters()
        {
            var Info = LoadChaptersInfo();

            var Langs = Info.Where(x => x.Type == "chapter").Select(x => x.Attributes.TranslatedLanguage).ToArray().Distinct();

            var TargetLang = SelectLanguage(Langs.ToArray());

            var AllChapters = Info
                .Where(x => x.Type == "chapter" && x.Attributes.TranslatedLanguage == TargetLang)
                .GroupBy(x => (x.Attributes.Volume??"") + "." + x.Attributes.Chapter)
                .Select(x=>x.First());

            var Query = AllChapters
                    .OrderByDescending(GetVolume)
                    .GroupBy(x => x.Attributes.Volume);
            

            if (Query.Count() > 1)
            {
                if (Info.Any(x => x.Attributes.Volume == null))
                {
                    var Chapters = AllChapters
                        .Select(x => x.Attributes.Chapter)
                        .OrderBy(x => x);
                    if (Chapters.Count() == Chapters.Distinct().Count())
                    {
                        Query = AllChapters.GroupBy(x => "");
                    }
                }
            }

            List<string> Names = new List<string>();
            foreach (var Volume in Query)
            {
                var SubQuery = Volume.OrderByDescending(GetChapter);

                foreach (var Chapter in SubQuery)
                {
                    string Name = GetChapterName(Chapter);

                    if (Names.Contains(Name))
                        continue;

                    Names.Add(Name);

                    int ID = ChapterInfo.Count;
                    ChapterInfo[ID] = Chapter.Id;

                    yield return new KeyValuePair<int, string>(ID, Name);
                }
            }
        }

        private float GetChapter(ChapterData x)
        {
            var Vol = x.Attributes.Chapter?.Trim().Replace(".", CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator);
            if (string.IsNullOrWhiteSpace(Vol) || !float.TryParse(Vol, out _))
                Vol = "99999";

            return float.Parse(Vol);
        }

        private float GetVolume(ChapterData x)
        {
                var Vol = x.Attributes.Volume?.Trim().Replace(".", CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator);
                if (string.IsNullOrWhiteSpace(Vol) || !float.TryParse(Vol, out _))
                    Vol = "99999";

                return float.Parse(Vol);
        }

        private static string GetChapterName(ChapterData Chapter)
        {
            string Name;

            if (string.IsNullOrWhiteSpace(Chapter.Attributes.Volume))
                Name = $"Ch. {Chapter.Attributes.Chapter}".Trim('.');
            else
                Name = $"Vol. {Chapter.Attributes.Volume} Ch. {Chapter.Attributes.Chapter}".Trim('.', ' ');
            return Name;
        }

        private static string LastLang = null;
        private string SelectLanguage(string[] Avaliable)
        {
            if (LastLang != null && Avaliable.Contains(LastLang))
                return LastLang;
            return LastLang = AccountTools.PromptOption("Select a Language", Avaliable.Distinct().Where(x=>!string.IsNullOrWhiteSpace(x)).ToArray());
        }

        public int GetChapterPageCount(int ID)
        {
            return GetChapterPages(ID).Length;
        }

        Dictionary<int, string[]> PagesCache = new Dictionary<int, string[]>();
        string[] GetChapterPages(int ID)
        {
            if (PagesCache.ContainsKey(ID))
                return PagesCache[ID];

            var ChapID = ChapterInfo[ID];
            var QueryURI = $"https://api.mangadex.org/at-home/server/{ChapID}";
            var Resp = Encoding.UTF8.GetString(QueryURI.Download(UserAgent: ProxyTools.UserAgent));

            var Info = JsonConvert.DeserializeObject<MangaChapterData>(Resp);

            return PagesCache[ID] = Info.Chapter.Data
                .Select(x => $"{Info.BaseUrl}/data/{Info.Chapter.Hash}/{x}")
                .ToArray();
        }

        string CurrentTitle = null;
        ChapterData[] Current = null;
        ChapterData[] LoadChaptersInfo()
        {
            if (CurrentTitle == ComicID && Current != null)
                return Current;

            CurrentTitle = ComicID;
            Current = null;

            List<ChapterData> Chaps = new List<ChapterData>();

            var QueryURI = $"https://api.mangadex.org/manga/{ComicID}/feed?limit=500&offset=";

            int Offset = 0;

            Feed Info;

            do
            {
                var Resp = Encoding.UTF8.GetString((QueryURI + Offset).Download(UserAgent: ProxyTools.UserAgent));

                Info = JsonConvert.DeserializeObject<Feed>(Resp);

                Chaps.AddRange(Info.Data);

                Offset += 500;
            } while (Chaps.Count < Info.Total);

            return Current = Chaps.ToArray();
        }

        public IDecoder GetDecoder()
        {
            return new CommonImage();
        }

        public PluginInfo GetPluginInfo()
        {
            return new PluginInfo()
            {
                Name = "Mangadex",
                Author = "Marcussacana",
                SupportComic = true,
                Version = new Version(2, 7)
            };
        }

        public bool IsValidPage(string HTML, Uri URL)
        {
            return false;
        }

        public bool IsValidUri(Uri Uri)
        {
            return Uri.Host.ToLower().Contains("mangadex.org")
                && Uri.PathAndQuery.ToLower().StartsWith("/title/");
        }

        public ComicInfo LoadUri(Uri Uri)
        {
            ComicID = Uri.PathAndQuery.Split('/')[2].Split('?')[0];

            var QueryURI = $"https://api.mangadex.org/manga/{ComicID}?&includes[]=cover_art";

            var Resp = Encoding.UTF8.GetString(QueryURI.Download(UserAgent: ProxyTools.UserAgent));

            var Info = JsonConvert.DeserializeObject<Manga>(Resp);

            var CoverID = Info.Data.Relationships.First(x => x.Type == "cover_art")
                          .Attributes.FileName;

            var CoverURI = $"https://uploads.mangadex.org/covers/{ComicID}/{CoverID}";

            var TitleInfo = Info.Data.Attributes.Title;
            return new ComicInfo()
            {
                Title = TitleInfo.En ?? TitleInfo.JaRo ?? TitleInfo.Ja_Ro ?? TitleInfo.Ja ?? TitleInfo.Ko,
                ContentType = ContentType.Comic,
                Cover = CoverURI.TryDownload(UserAgent: ProxyTools.UserAgent),
                Url = Uri
            };
        }

        string ComicID;

        [DebuggerDisplay("V. {Attributes.Volume} Ch. {Attributes.Chapter}")]
        public struct ChapterData
        {
            public string Id { get; set; }
            public string Type { get; set; }
            public ChapterAttributes Attributes { get; set; }
            public ChapterRelationship[] Relationships { get; set; }
        }

        public struct ChapterAttributes
        {
            public string Volume { get; set; }
            public string Chapter { get; set; }
            public string Title { get; set; }
            public string TranslatedLanguage { get; set; }
            public object ExternalUrl { get; set; }
            public DateTime PublishAt { get; set; }
            public DateTime ReadableAt { get; set; }
            public DateTime CreatedAt { get; set; }
            public DateTime UpdatedAt { get; set; }
            public int Pages { get; set; }
            public int Version { get; set; }
        }

        public struct ChapterRelationship
        {
            public string Id { get; set; }
            public string Type { get; set; }
        }

        public struct Feed
        {
            public string Result { get; set; }
            public string Response { get; set; }
            public ChapterData[] Data { get; set; }
            public int Limit { get; set; }
            public int Offset { get; set; }
            public int Total { get; set; }
        }


        public struct LocalizatedString
        {
            public string En { get; set; }
            [JsonProperty("ja-ro")]
            public string Ja_Ro { get; set; }
            public string JaRo { get; set; }
            public string Ja { get; set; }
            public string Ko { get; set; }
        }

        public struct MangaAttributesLinks
        {
            public string Al { get; set; }
            public string Ap { get; set; }
            public string Bw { get; set; }
            public string Mu { get; set; }
            public string Nu { get; set; }
            public string Amz { get; set; }
            public string Mal { get; set; }
            public string Raw { get; set; }
            public string Engtl { get; set; }
        }

        public struct MangaAttributes
        {
            public LocalizatedString Title { get; set; }
            public List<LocalizatedString> AltTitles { get; set; }
            public LocalizatedString Description { get; set; }
            public bool IsLocked { get; set; }
            public MangaAttributesLinks Links { get; set; }
            public string OriginalLanguage { get; set; }
            public string LastVolume { get; set; }
            public string LastChapter { get; set; }
            public string PublicationDemographic { get; set; }
            public string Status { get; set; }
            public int? Year { get; set; }
            public string ContentRating { get; set; }
            public List<MangaTag> Tags { get; set; }
            public string State { get; set; }
            public bool ChapterNumbersResetOnNewVolume { get; set; }
            public DateTime CreatedAt { get; set; }
            public DateTime UpdatedAt { get; set; }
            public int Version { get; set; }
            public List<string> AvailableTranslatedLanguages { get; set; }
            public string LatestUploadedChapter { get; set; }
        }

        public struct MangaTagAttributes
        {
            public LocalizatedString Name { get; set; }
        }

        public struct MangaTag
        {
            public string Id { get; set; }
            public string Type { get; set; }
            public MangaTagAttributes Attributes { get; set; }
            public List<MangaRelationship> Relationships { get; set; }
        }

        public struct MangaRelationship
        {
            public string Id { get; set; }
            public string Type { get; set; }
            public MangaRelationshipAttributes Attributes { get; set; }
        }

        public struct MangaRelationshipAttributes
        {
            public string Description { get; set; }
            public string Volume { get; set; }
            public string FileName { get; set; }
            public string Locale { get; set; }
            public DateTime CreatedAt { get; set; }
            public DateTime UpdatedAt { get; set; }
            public int Version { get; set; }
        }

        public struct MangaData
        {
            public string Id { get; set; }
            public string Type { get; set; }
            public MangaAttributes Attributes { get; set; }
            public List<MangaRelationship> Relationships { get; set; }
        }

        public struct MangaRelationships
        {
            public string Id { get; set; }
            public string Type { get; set; }
        }

        public struct Manga
        {
            public string Result { get; set; }
            public string Response { get; set; }
            public MangaData Data { get; set; }
            public List<MangaRelationships> Relationships { get; set; }
        }
        public struct MangaChapterData
        {
            public string Result { get; set; }
            public string BaseUrl { get; set; }
            public MangaChapter Chapter { get; set; }
        }

        public struct MangaChapter
        {
            public string Hash { get; set; }
            public string[] Data { get; set; }
            public string[] DataSaver { get; set; }
        }
    }
}