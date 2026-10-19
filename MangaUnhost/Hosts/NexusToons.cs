using MangaUnhost.Browser;
using MangaUnhost.Decoders;
using MangaUnhost.Others;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace MangaUnhost.Hosts
{
    public class NexusToons : IHost
    {
        private const string SiteBase = "https://nexustoons.com";
        private const string FallbackCryptoSecret = "OrionNexus2025CryptoKey!Secure";
        private const string ModernUserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36";

        private static readonly object SecretLock = new object();
        private static string cachedSecret = null;
        private static OrionCrypto cachedCrypto = null;

        private readonly Dictionary<int, ChapterEntry> chapterMap = new Dictionary<int, ChapterEntry>();
        private readonly Dictionary<int, string[]> pageMap = new Dictionary<int, string[]>();

        private Uri currentSeriesUrl;
        private string currentSlug;

        public NovelChapter DownloadChapter(int ID)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<byte[]> DownloadPages(int ID)
        {
            var referer = currentSeriesUrl?.AbsoluteUri ?? (SiteBase + "/");
            foreach (var page in GetChapterPages(ID))
            {
                var data = DownloadBinary(page, referer);
                if (data != null && data.Length > 0)
                    yield return data;
            }
        }

        public IEnumerable<KeyValuePair<int, string>> EnumChapters()
        {
            if (!chapterMap.Any())
                LoadChapterMap();

            foreach (var chapter in chapterMap.OrderByDescending(x => x.Value.SortNumber).ThenByDescending(x => x.Key))
                yield return new KeyValuePair<int, string>(chapter.Key, chapter.Value.Name);
        }

        public int GetChapterPageCount(int ID)
        {
            return GetChapterPages(ID).Length;
        }

        public IDecoder GetDecoder()
        {
            return new CommonImage();
        }

        public PluginInfo GetPluginInfo()
        {
            return new PluginInfo()
            {
                Name = "Nexus Toons",
                Author = "Marcussacana",
                SupportComic = true,
                SupportNovel = false,
                GenericPlugin = false,
                Version = new Version(2, 0, 0)
            };
        }

        public bool IsValidPage(string HTML, Uri URL)
        {
            if (!IsValidUri(URL))
                return false;

            return !string.IsNullOrWhiteSpace(HTML) &&
                   (HTML.IndexOf("Nexus", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    HTML.IndexOf("nexustoons", StringComparison.OrdinalIgnoreCase) >= 0);
        }

        public bool IsValidUri(Uri Uri)
        {
            if (Uri == null || string.IsNullOrWhiteSpace(Uri.Host))
                return false;

            if (!Uri.Host.Equals("nexustoons.com", StringComparison.OrdinalIgnoreCase) &&
                !Uri.Host.EndsWith(".nexustoons.com", StringComparison.OrdinalIgnoreCase))
                return false;

            var path = Uri.AbsolutePath.ToLowerInvariant();
            return path.StartsWith("/manga/") || path.StartsWith("/ler/") || path.StartsWith("/r/");
        }

        public ComicInfo LoadUri(Uri Uri)
        {
            chapterMap.Clear();
            pageMap.Clear();

            currentSlug = ExtractSlug(Uri);
            if (string.IsNullOrWhiteSpace(currentSlug))
                throw new Exception($"Não foi possível identificar a obra pela URL: {Uri}");

            currentSeriesUrl = new Uri($"{SiteBase}/manga/{currentSlug}");

            var data = ApiRequest($"/api/manga/{currentSlug}") as JObject;
            if (data == null)
                throw new Exception($"Falha ao carregar informações da obra: {currentSlug}");

            string title = data["title"]?.ToString() ?? currentSlug;
            string coverUrl = data["coverImage"]?.ToString() ??
                              data["cover_url"]?.ToString() ??
                              data["cover"]?.ToString();

            byte[] coverData = null;
            if (!string.IsNullOrEmpty(coverUrl))
                coverData = DownloadBinary(coverUrl, currentSeriesUrl.AbsoluteUri);

            ParseChapters(data["chapters"] as JArray);

            return new ComicInfo()
            {
                Title = title,
                Cover = coverData,
                ContentType = ContentType.Comic,
                Url = currentSeriesUrl
            };
        }

        private void LoadChapterMap()
        {
            if (string.IsNullOrWhiteSpace(currentSlug))
                throw new Exception("Obra não carregada.");

            var data = ApiRequest($"/api/manga/{currentSlug}") as JObject;
            if (data == null)
                throw new Exception($"Falha ao recarregar capítulos da obra: {currentSlug}");

            ParseChapters(data["chapters"] as JArray);
        }

        private void ParseChapters(JArray chapters)
        {
            chapterMap.Clear();
            if (chapters == null)
                return;

            int id = 0;
            foreach (var item in chapters)
            {
                int chapId = item["id"]?.Value<int>() ?? 0;
                string numStr = item["number"]?.ToString() ?? id.ToString();
                double.TryParse(numStr, NumberStyles.Any, CultureInfo.InvariantCulture, out double sortNum);

                string rawTitle = item["title"]?.ToString();
                string name = FormatChapterName(numStr, rawTitle);

                chapterMap[id] = new ChapterEntry
                {
                    InternalId = id,
                    ChapterId = chapId,
                    Number = numStr,
                    Name = name,
                    SortNumber = sortNum,
                    Url = $"{SiteBase}/ler/{currentSlug}/{chapId}"
                };
                id++;
            }
        }

        private string[] GetChapterPages(int ID)
        {
            if (pageMap.ContainsKey(ID))
                return pageMap[ID];

            if (!chapterMap.ContainsKey(ID))
                LoadChapterMap();

            if (!chapterMap.ContainsKey(ID))
                throw new Exception($"Capítulo {ID} não encontrado.");

            var chapter = chapterMap[ID];
            var data = ApiRequest($"/api/chapter/{chapter.ChapterId}") as JObject;
            if (data == null)
                data = ApiRequest($"/api/read/{chapter.ChapterId}") as JObject;

            if (data == null)
                throw new Exception($"Falha ao carregar páginas do capítulo {chapter.ChapterId}.");

            var pages = new List<string>();
            var pagesArr = data["pages"] as JArray;
            if (pagesArr != null)
            {
                foreach (var p in pagesArr)
                {
                    string url = p["imageUrl"]?.ToString() ?? p["url"]?.ToString() ?? p.ToString();
                    if (!string.IsNullOrWhiteSpace(url))
                        pages.Add(url);
                }
            }

            var result = pages.ToArray();
            pageMap[ID] = result;
            return result;
        }

        private static string FormatChapterName(string number, string title)
        {
            if (string.IsNullOrWhiteSpace(title))
                return number;

            title = title.Trim();
            if (title.Equals(number, StringComparison.OrdinalIgnoreCase))
                return number;

            return $"{number} - {title}";
        }

        private string ExtractSlug(Uri uri)
        {
            if (uri == null)
                return null;

            var path = uri.AbsolutePath.Trim('/');
            var parts = path.Split('/');

            if (parts.Length >= 2)
            {
                if (parts[0].Equals("manga", StringComparison.OrdinalIgnoreCase))
                    return parts[1];

                if (parts[0].Equals("ler", StringComparison.OrdinalIgnoreCase))
                    return parts[1];

                if (parts[0].Equals("r", StringComparison.OrdinalIgnoreCase))
                {
                    string hash = parts[1];
                    var data = ApiRequest($"/api/chapter/{hash}") as JObject ??
                               ApiRequest($"/api/read/{hash}") as JObject;

                    var slug = data?["manga"]?["slug"]?.ToString();
                    if (!string.IsNullOrWhiteSpace(slug))
                        return slug;
                }
            }

            return null;
        }

        private static JToken ApiRequest(string path, bool retryOnDecryptFail = true)
        {
            EnsureCrypto();

            string json;
            using (var client = new HttpClient())
            {
                client.Timeout = TimeSpan.FromSeconds(30);
                client.DefaultRequestHeaders.Add("User-Agent", ModernUserAgent);
                client.DefaultRequestHeaders.Add("Referer", SiteBase + "/");
                client.DefaultRequestHeaders.Add("Accept", "application/json, text/plain, */*");

                var resp = client.GetAsync($"{SiteBase}{path}").Result;
                resp.EnsureSuccessStatusCode();
                json = resp.Content.ReadAsStringAsync().Result;
            }

            try
            {
                return cachedCrypto.ProcessResponse(json);
            }
            catch
            {
                if (retryOnDecryptFail)
                {
                    EnsureCrypto(forceRefresh: true);
                    return cachedCrypto.ProcessResponse(json);
                }
                throw;
            }
        }

        private static void EnsureCrypto(bool forceRefresh = false)
        {
            lock (SecretLock)
            {
                if (!forceRefresh && cachedCrypto != null && cachedCrypto.Initialized)
                    return;

                if (!forceRefresh)
                {
                    string saved = Ini.GetConfig("NexusToons", "CryptoSecret", Main.SettingsPath, false);
                    if (!string.IsNullOrEmpty(saved))
                    {
                        cachedSecret = saved;
                        cachedCrypto = new OrionCrypto();
                        cachedCrypto.InitFromSecret(cachedSecret);
                        return;
                    }
                }

                string siteSecret = DumpSecretFromSite();
                cachedSecret = !string.IsNullOrEmpty(siteSecret) ? siteSecret : FallbackCryptoSecret;

                cachedCrypto = new OrionCrypto();
                cachedCrypto.InitFromSecret(cachedSecret);

                Ini.SetConfig("NexusToons", "CryptoSecret", cachedSecret, Main.SettingsPath);
            }
        }

        private static string DumpSecretFromSite()
        {
            try
            {
                using (var client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(15);
                    client.DefaultRequestHeaders.Add("User-Agent", ModernUserAgent);
                    string html = client.GetStringAsync(SiteBase).Result;

                    var match = Regex.Match(html, @"src=[""']?(/assets/[^""'\s>]+\.js)[""']?");
                    if (match.Success)
                    {
                        string jsUrl = SiteBase + match.Groups[1].Value;
                        string js = client.GetStringAsync(jsUrl).Result;

                        var secretMatch = Regex.Match(js, @"[""'](Orion[A-Za-z0-9_!@#$%^&*]+)[""']");
                        if (secretMatch.Success)
                            return secretMatch.Groups[1].Value.Trim();

                        var fnMatch = Regex.Match(js, @"const\s+[a-zA-Z0-9_$]+\s*=\s*[""']([^""']+)[""'];\s*return\s+(?:await\s+)?[a-zA-Z0-9_$]+\.initFromSecret");
                        if (fnMatch.Success)
                            return fnMatch.Groups[1].Value.Trim();
                    }
                }
            }
            catch { }

            return null;
        }

        private byte[] DownloadBinary(string url, string referer)
        {
            if (string.IsNullOrWhiteSpace(url))
                return null;

            try
            {
                var data = url.TryDownload(
                    Referer: referer ?? (SiteBase + "/"),
                    UserAgent: ModernUserAgent,
                    isImage: true);

                if (data != null && data.Length > 0)
                    return data;

                using (var client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(30);
                    client.DefaultRequestHeaders.Add("Referer", referer ?? (SiteBase + "/"));
                    client.DefaultRequestHeaders.Add("User-Agent", ModernUserAgent);
                    return client.GetByteArrayAsync(url).Result;
                }
            }
            catch
            {
                return null;
            }
        }

        private sealed class ChapterEntry
        {
            public int InternalId { get; set; }
            public int ChapterId { get; set; }
            public string Number { get; set; }
            public string Name { get; set; }
            public double SortNumber { get; set; }
            public string Url { get; set; }
        }

        internal class OrionCrypto
        {
            private class KeyState
            {
                public byte[] Key = Array.Empty<byte>();
                public byte[] SBox = new byte[256];
                public byte[] RSBox = new byte[256];
            }

            private readonly List<KeyState> keys = new List<KeyState>();
            public bool Initialized => keys.Count == 5;

            public void InitFromSecret(string secret)
            {
                keys.Clear();
                var keysHex = new List<string>();
                using (var sha256 = SHA256.Create())
                {
                    for (int n = 0; n < 5; n++)
                    {
                        string a = $"_orion_key_{n}_v2_{secret}";
                        byte[] hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(a));
                        string hex = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                        keysHex.Add(hex);
                    }
                }
                Init(keysHex);
            }

            private void Init(List<string> keysHex)
            {
                if (keysHex == null || keysHex.Count != 5)
                    throw new Exception("São necessárias exatamente 5 chaves.");

                foreach (var hex in keysHex)
                {
                    byte[] bytes = new byte[hex.Length / 2];
                    for (int i = 0; i < bytes.Length; i++)
                    {
                        bytes[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
                    }

                    var ks = new KeyState { Key = bytes };
                    InitSBoxForKey(ks);
                    keys.Add(ks);
                }
            }

            private static void InitSBoxForKey(KeyState ks)
            {
                byte[] t = ks.Key;
                for (int a = 0; a < 256; a++)
                    ks.SBox[a] = (byte)a;

                int n = 0;
                for (int a = 0; a < 256; a++)
                {
                    n = (n + ks.SBox[a] + t[a % t.Length]) % 256;
                    byte tmp = ks.SBox[a];
                    ks.SBox[a] = ks.SBox[n];
                    ks.SBox[n] = tmp;
                }

                for (int a = 0; a < 256; a++)
                {
                    ks.RSBox[ks.SBox[a]] = (byte)a;
                }
            }

            private static byte RotateRight(byte e, int t)
            {
                t %= 8;
                if (t == 0) return e;
                return (byte)(((e >> t) | (e << (8 - t))) & 0xFF);
            }

            public string Decrypt(int keyIndex, string ciphertextB64)
            {
                if (!Initialized)
                    throw new Exception("OrionCrypto não inicializado.");
                if (keyIndex < 0 || keyIndex >= keys.Count)
                    throw new Exception($"Índice de chave inválido: {keyIndex}");

                var ks = keys[keyIndex];
                byte[] a = ks.Key;
                byte[] r = ks.RSBox;
                byte[] raw = Convert.FromBase64String(ciphertextB64);
                byte[] s = new byte[raw.Length];
                int l = a.Length;

                for (int c = raw.Length - 1; c >= 0; c--)
                {
                    int e = raw[c];
                    e ^= (c > 0 ? raw[c - 1] : a[l - 1]);
                    e = r[e & 0xFF];
                    int t = ((a[(c + 3) % l] + (255 & c)) & 255) % 7 + 1;
                    e = RotateRight((byte)e, t);
                    e ^= a[c % l];
                    s[c] = (byte)e;
                }

                return Encoding.UTF8.GetString(s);
            }

            public JToken ProcessResponse(string json)
            {
                if (string.IsNullOrWhiteSpace(json))
                    return null;

                var token = JToken.Parse(json);
                if (token is JObject obj && obj["d"] != null && obj["d"].Type == JTokenType.String)
                {
                    int v = obj["v"]?.Value<int>() ?? 0;
                    int k = (v == 1) ? 0 : (obj["k"]?.Value<int>() ?? 0);
                    string d = obj["d"].ToString();
                    string plain = Decrypt(k, d);
                    return JToken.Parse(plain);
                }

                return token;
            }
        }
    }
}
