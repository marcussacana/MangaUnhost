using HtmlAgilityPack;
using MangaUnhost.Browser;
using MangaUnhost.Others;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

namespace MangaUnhost.Hosts {
    class KissManga : IHost {

        HtmlDocument CurrentChapter;
        HtmlDocument Document;
        ComicInfo CurrentTitle;
        Dictionary<int, string> ChapterNames = new Dictionary<int, string>();
        Dictionary<int, string> ChapterLinks = new Dictionary<int, string>();
        Dictionary<int, string> ChapterPages = new Dictionary<int, string>();

        static CloudflareData? Cloudflare;

        public NovelChapter DownloadChapter(int ID) {
            throw new NotImplementedException();
        }

        public IEnumerable<byte[]> DownloadPages(int ID) {
            foreach (var PageUrl in GetChapterPages(ID)) {
                yield return Download(new Uri(PageUrl));
            }
        }

        public IEnumerable<KeyValuePair<int, string>> EnumChapters() {
            int ID = ChapterLinks.Count;

            foreach (var Node in Document.SelectNodes("//table[@class=\"listing\"]//a")) {

                ChapterLinks[ID] = Node.GetAttributeValue("href", string.Empty).EnsureAbsoluteUrl("https://kissmanga.com");
                ChapterNames[ID] = DataTools.GetRawName(GetChapterName(ChapterLinks[ID]));

                yield return new KeyValuePair<int, string>(ID, ChapterNames[ID++]);
            }
        }

        // KissManga non-sense chapter namming soluction
        public string GetChapterName(string ChapterURL) {
            const string Prefix = "/manga/";
            string Part = ChapterURL.Substring(ChapterURL.ToLower().IndexOf(Prefix) + Prefix.Length);
            Part = Part.Between('/', '?');
            if (Part.ToLower().Contains("ch-"))
                Part = Part.Substring("ch-");
            string Rst = string.Empty;
            foreach (char c in Part) {
                if (Rst == string.Empty && !char.IsNumber(c))
                    continue;
                if (c == '-') {
                    Rst += '.';
                    continue;
                }
                if (!char.IsNumber(c))
                    break;
                Rst += c;
            }
            return string.Join(".", (from x in Rst.Trim('.').Split('.') select x.TrimStart('0')).ToArray());
        }

        public int GetChapterPageCount(int ID) {
            return GetChapterPages(ID).Length;
        }
        private string[] GetChapterPages(int ID) {
            var Page = CurrentChapter = GetChapterHtml(ID);

            var Script = Page.DocumentNode.SelectSingleNode("//script[contains(., \"lstImages\")]").InnerHtml;

            var Matches = Regex.Matches(Script, "wrapKA\\(\"(.*)\\\"\\)")
                .Cast<Match>()
                .SelectMany(x => x.Captures.Cast<Capture>()
                .Select(y => y.Value.Between('"', '"')).ToArray()).ToArray();

            List<string> Pages = new List<string>();

            foreach (var B64 in Matches) {
                string Link = DecryptAesB64(B64);
                Pages.Add(Link.SkipProtectors());
            }

            return Pages.ToArray();
        }

        private string IV = "a5 e8 e2 e9 c2 72 1b e0 a8 4a d6 60 c4 72 c1 f3";
        private string _key = null;
        private string Key {
            get {
                if (_key != null)
                    return _key;

                string Key = "mshsdf832nsdbash20asdm";
                
                string chko = Key;
                var Nodes = CurrentChapter.DocumentNode.SelectNodes("//script[contains(., \"chko\")]");
                if (Nodes != null)
                    foreach (var Node in Nodes) {
                        string Script = Node.InnerHtml.Trim('\r', '\n', ' ', '\t', ';') + ";\r\nchko;";
                        chko = (string)JSTools.EvaluateScript(Script.Replace("key = CryptoJS.SHA256(chko);", ""));
                    }
                _key = chko;
                return _key;
            }
        }
        private string DecryptAesB64(string Content) {
            var SHA = new SHA256Managed();
            byte[] Key = SHA.ComputeHash(Encoding.ASCII.GetBytes(this.Key));
            var AES = new AesManaged {
                Key = Key,
                IV = (from x in IV.Split(' ') select Convert.ToByte(x, 16)).ToArray(),
                Mode = CipherMode.CBC,
                Padding = PaddingMode.PKCS7
            };

            var data = Convert.FromBase64String(Content);
            using (MemoryStream Input = new MemoryStream(data))
            using (MemoryStream Stream = new MemoryStream())
            using (CryptoStream Decrypor = new CryptoStream(Stream, AES.CreateDecryptor(), CryptoStreamMode.Write)) {
                Input.CopyTo(Decrypor);
                Decrypor.FlushFinalBlock();
                return Encoding.UTF8.GetString(Stream.ToArray()).Replace("\x10", "").Replace("\x0f", "");
            }
        }

        private HtmlDocument GetChapterHtml(int ID) {
            if (ChapterPages.ContainsKey(ID)) {
                HtmlDocument Document = new HtmlDocument();
                Document.LoadHtml(ChapterPages[ID]);
                return Document;
            }

            ChapterPages[ID] = Encoding.UTF8.GetString(TryDownload(new Uri(ChapterLinks[ID])));

            return GetChapterHtml(ID);
        }

        public IDecoder GetDecoder() {
            return new Decoders.CommonImage();
        }

        public PluginInfo GetPluginInfo() {
            return new PluginInfo() {
                Name = "KissManga",
                Author = "Marcussacana",
                SupportComic = true,
                SupportNovel = false,
                Version = new Version(1, 0)
            };
        }

        public bool IsValidUri(Uri Uri) {
            return Uri.Host.ToLower().Contains("kissmanga") && Uri.AbsolutePath.ToLower().Contains("/manga/");
        }

        public ComicInfo LoadUri(Uri Uri) {
            string CurrentHtml = Encoding.UTF8.GetString(TryDownload(Uri));
             if (CurrentHtml.IsCloudflareTriggered()) {
                Cloudflare = JSTools.BypassCloudflare(Uri.AbsoluteUri);
                CurrentHtml = Encoding.UTF8.GetString(TryDownload(Uri));
            }

            Document = new HtmlDocument();
            Document.LoadHtml(CurrentHtml);

            ComicInfo Info = new ComicInfo();
            Info.Title = Document.SelectSingleNode("//a[@class=\"bigChar\"]").InnerText;
            Info.Title = HttpUtility.HtmlDecode(Info.Title);

            Info.Cover = TryDownload(new Uri(Document
                .SelectSingleNode("//*[@id=\"rightside\"]//img")
                .GetAttributeValue("src", "")));

            Info.ContentType = ContentType.Comic;

            CurrentTitle = Info;

            return Info;
        }

        public byte[] TryDownload(Uri URL) {
            if (Cloudflare == null)
                return URL.TryDownload(AcceptableErrors: new System.Net.WebExceptionStatus[] { System.Net.WebExceptionStatus.ProtocolError });
            else
                return URL.TryDownload(UserAgent: Cloudflare?.UserAgent, Cookie: Cloudflare?.Cookies);
        }
        public byte[] Download(Uri URL) {
            if (Cloudflare == null)
                return URL.TryDownload(AcceptableErrors: new System.Net.WebExceptionStatus[] { System.Net.WebExceptionStatus.ProtocolError }) ?? throw new Exception("Failed to Download");
            else
                return URL.TryDownload(UserAgent: Cloudflare?.UserAgent, Cookie: Cloudflare?.Cookies) ?? throw new Exception("Failed to Download");
        }
        public bool IsValidPage(string HTML, Uri URL) => false;
    }
}
