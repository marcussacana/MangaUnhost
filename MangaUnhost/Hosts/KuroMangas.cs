using CefSharp;
using CefSharp.OffScreen;
using MangaUnhost.Browser;
using MangaUnhost.Decoders;
using MangaUnhost.Others;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace MangaUnhost.Hosts
{
    public class KuroMangas : IHost
    {
        private const string SiteBase = "https://kuromangas.com";
        private const string CdnBase = "https://cdn.kuromangas.com";
        private const string DefaultApiEncryptionKey = "i67ato8l6sai74jyIHfE2oMmieshoforanuYTusF4jKdqEwhUEft9dsadcxzsaipnjm8";
        private const string HostnamePart = "kuromangas.com::v2";
        private const string Antibot = "x9_4v2_b";

        private static readonly string[] SupportedHosts = new[]
        {
            "kuromangas.com",
            "www.kuromangas.com"
        };

        private const string DefaultEmail = "dummy@gmail.com";
        private const string DefaultPassword = "123Dummy456";

        private static readonly object AuthLock = new object();
        private static string cachedKnToken = null;
        private static string cachedKuroSession = null;
        private static string cachedApiEncryptionKey = DefaultApiEncryptionKey;

        private Uri currentSeriesUrl;
        private int currentMangaId;
        private JObject currentMangaData;

        private readonly Dictionary<int, ChapterInfo> chapterMap = new Dictionary<int, ChapterInfo>();
        private readonly Dictionary<int, string[]> pageMap = new Dictionary<int, string[]>();

        public NovelChapter DownloadChapter(int ID)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<byte[]> DownloadPages(int ID)
        {
            var chapter = chapterMap.ContainsKey(ID) ? chapterMap[ID] : null;
            var referer = chapter?.Url ?? currentSeriesUrl?.AbsoluteUri ?? (SiteBase + "/");

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
                Name = "Kuro Mangás",
                Author = "Marcussacana",
                SupportComic = true,
                SupportNovel = false,
                GenericPlugin = false,
                Version = new Version(1, 0, 0)
            };
        }

        public bool IsValidPage(string HTML, Uri URL)
        {
            if (!IsValidUri(URL))
                return false;

            return !string.IsNullOrWhiteSpace(HTML) &&
                   (HTML.Contains("Kuro Mangás") || HTML.Contains("kuromangas"));
        }

        public bool IsValidUri(Uri Uri)
        {
            if (Uri == null || !IsSupportedHost(Uri))
                return false;

            int mangaId = ExtractMangaId(Uri);
            return mangaId > 0;
        }

        public ComicInfo LoadUri(Uri Uri)
        {
            chapterMap.Clear();
            pageMap.Clear();

            currentSeriesUrl = ResolveSeriesUri(Uri);
            currentMangaId = ExtractMangaId(currentSeriesUrl);
            if (currentMangaId <= 0)
                throw new Exception($"Não foi possível extrair o ID do mangá a partir da URL: {Uri}");

            EnsureAuthenticated();

            var json = ApiRequest($"/api/mangas/{currentMangaId}");
            var data = JObject.Parse(json);
            currentMangaData = data;

            var manga = data["manga"] as JObject;
            string title = manga?["title"]?.ToString() ?? $"Kuro Manga #{currentMangaId}";
            string coverUrl = FormatPageUrl(manga?["cover_image"]?.ToString() ?? manga?["cover_url"]?.ToString());

            byte[] coverData = null;
            if (!string.IsNullOrEmpty(coverUrl))
            {
                coverData = DownloadBinary(coverUrl, currentSeriesUrl.AbsoluteUri);
            }

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
            if (currentMangaId <= 0)
                throw new Exception("Mangá não carregado.");

            EnsureAuthenticated();

            var json = ApiRequest($"/api/mangas/{currentMangaId}");
            var data = JObject.Parse(json);
            currentMangaData = data;

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
                string chapNumStr = item["chapter_number"]?.ToString() ?? id.ToString();
                double numero = 0;
                double.TryParse(chapNumStr, NumberStyles.Any, CultureInfo.InvariantCulture, out numero);

                string rawTitle = item["title"]?.ToString();
                string name = FormatChapterName(numero, rawTitle);
                string chapUrl = $"{SiteBase}/read/{currentMangaId}/{chapId}";

                chapterMap[id] = new ChapterInfo
                {
                    InternalId = id,
                    ChapterId = chapId,
                    Numero = numero,
                    Name = name,
                    Titulo = rawTitle ?? name,
                    Url = chapUrl,
                    SortNumber = numero
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

            EnsureAuthenticated();

            var chapter = chapterMap[ID];
            var json = ApiRequest($"/api/chapters/{chapter.ChapterId}");
            var data = JObject.Parse(json);

            var pages = new List<string>();
            var pagesArr = data["pages"] as JArray;
            if (pagesArr != null)
            {
                foreach (var p in pagesArr)
                {
                    string rawUrl = p?.ToString();
                    string formatted = FormatPageUrl(rawUrl);
                    if (!string.IsNullOrEmpty(formatted))
                        pages.Add(formatted);
                }
            }

            var result = pages.ToArray();
            pageMap[ID] = result;
            return result;
        }

        private static string FormatPageUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return string.Empty;

            if (url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                url.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
                url.StartsWith("blob:", StringComparison.OrdinalIgnoreCase) ||
                url.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
                return url;

            string path = url.StartsWith("/uploads/") ? url.Substring("/uploads/".Length - 1) : url;
            if (!path.StartsWith("/"))
                path = "/" + path;

            return $"{CdnBase}{path}";
        }

        private static string FormatChapterName(double numero, string titulo)
        {
            string numStr = numero.ToString(CultureInfo.InvariantCulture);
            if (string.IsNullOrWhiteSpace(titulo))
                return numStr;

            string cleaned = titulo.Trim();
            string numPattern = "(?:0*)?" + Regex.Escape(numStr).Replace(@"\.", @"[.,]");
            var match = Regex.Match(cleaned, @"^(?:cap[íi]tulo|cap\.?|ch\.?)\s*" + numPattern + @"(?:\s*[:\-–—]\s*(.*))?$", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                string extra = match.Groups[1].Value.Trim();
                if (!string.IsNullOrEmpty(extra))
                    return $"{numStr} - {extra}";
                return numStr;
            }

            cleaned = Regex.Replace(cleaned, @"^(?:cap[íi]tulo|cap\.?|ch\.?)\s*", "", RegexOptions.IgnoreCase).Trim();
            if (string.IsNullOrEmpty(cleaned))
                return numStr;

            return cleaned;
        }

        private byte[] DownloadBinary(string url, string referer)
        {
            if (string.IsNullOrWhiteSpace(url))
                return null;

            try
            {
                var data = url.TryDownload(
                    Referer: referer ?? (SiteBase + "/"),
                    UserAgent: ProxyTools.UserAgent,
                    isImage: true);

                if (data != null && data.Length > 0)
                    return data;

                data = DownloadDirect(url, referer);
                if (data != null && data.Length > 0)
                    return data;

                return null;
            }
            catch
            {
                return null;
            }
        }

        private byte[] DownloadDirect(string url, string referer)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(30);
                    client.DefaultRequestHeaders.Add("Referer", referer ?? (SiteBase + "/"));
                    client.DefaultRequestHeaders.Add("User-Agent", ProxyTools.UserAgent);
                    return client.GetByteArrayAsync(url).Result;
                }
            }
            catch
            {
                return null;
            }
        }

        private static string ApiRequest(string path, string method = "GET", string jsonBody = null, bool retryAuth = true)
        {
            try
            {
                using (var handler = new HttpClientHandler { UseCookies = false })
                using (var client = new HttpClient(handler))
                {
                    client.Timeout = TimeSpan.FromSeconds(30);
                    var req = new HttpRequestMessage(new HttpMethod(method), $"{SiteBase}{path}");

                    req.Headers.Add("Accept", "application/json, text/plain, */*");
                    req.Headers.Add("User-Agent", ProxyTools.UserAgent);
                    req.Headers.Add("Referer", $"{SiteBase}/");
                    req.Headers.Add("Origin", SiteBase);

                    if (!string.IsNullOrEmpty(cachedKnToken))
                        req.Headers.Add("X-Client-Token", cachedKnToken);

                    string cookieHeader = BuildCookieHeader();
                    if (!string.IsNullOrEmpty(cookieHeader))
                        req.Headers.Add("Cookie", cookieHeader);

                    if (!string.IsNullOrEmpty(jsonBody) && method != "GET")
                        req.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

                    var resp = client.SendAsync(req).Result;

                    if ((resp.StatusCode == HttpStatusCode.Unauthorized || resp.StatusCode == HttpStatusCode.Forbidden) && retryAuth)
                    {
                        EnsureAuthenticated(forceRefresh: true);
                        return ApiRequest(path, method, jsonBody, retryAuth: false);
                    }

                    resp.EnsureSuccessStatusCode();

                    string dataKey = null;
                    if (resp.Headers.TryGetValues("x-kuro-datakey", out var keyVals))
                        dataKey = keyVals.FirstOrDefault();

                    string body = resp.Content.ReadAsStringAsync().Result;
                    if (body.Contains("\"_v_secure\""))
                    {
                        var obj = JObject.Parse(body);
                        string vSecure = obj["_v_secure"]?.ToString();
                        if (!string.IsNullOrEmpty(vSecure))
                        {
                            string decrypted = DecryptPayload(vSecure, dataKey);
                            if (!string.IsNullOrEmpty(decrypted))
                                return decrypted;
                        }
                    }

                    return body;
                }
            }
            catch (Exception)
            {
                if (retryAuth)
                {
                    EnsureAuthenticated(forceRefresh: true);
                    return ApiRequest(path, method, jsonBody, retryAuth: false);
                }
                throw;
            }
        }

        private static void EnsureAuthenticated(bool forceRefresh = false)
        {
            lock (AuthLock)
            {
                if (!forceRefresh && !string.IsNullOrEmpty(cachedKnToken) && ValidateCurrentSession())
                    return;

                // 1. Tentar credenciais configuradas em AccountTools
                try
                {
                    var accounts = AccountTools.LoadAccounts("KuroMangas");
                    if (accounts != null && accounts.Length > 0)
                    {
                        var acc = accounts[0];
                        string email = !string.IsNullOrEmpty(acc.Email) ? acc.Email : acc.Login;
                        string pass = acc.Password;
                        if (!string.IsNullOrEmpty(email) && !string.IsNullOrEmpty(pass))
                        {
                            if (TryLogin(email, pass))
                                return;
                        }
                    }
                }
                catch { }

                // 2. Tentar tokens persistidos em MangaUnhost.ini
                string savedKn = Ini.GetConfig("KuroMangas", "Token", Main.SettingsPath, false);
                string savedSess = Ini.GetConfig("KuroMangas", "Session", Main.SettingsPath, false);
                if (!string.IsNullOrEmpty(savedKn))
                {
                    cachedKnToken = savedKn;
                    cachedKuroSession = savedSess;
                    if (ValidateCurrentSession())
                        return;
                }

                // 3. Tentar credenciais padrão (dummy account)
                if (TryLogin(DefaultEmail, DefaultPassword))
                    return;

                // 4. Fallback: Login interativo via BrowserPopup
                LoginViaBrowser();
            }
        }

        private static bool ValidateCurrentSession()
        {
            if (string.IsNullOrEmpty(cachedKnToken))
                return false;

            try
            {
                using (var handler = new HttpClientHandler { UseCookies = false })
                using (var client = new HttpClient(handler))
                {
                    client.Timeout = TimeSpan.FromSeconds(8);
                    var req = new HttpRequestMessage(HttpMethod.Get, $"{SiteBase}/api/users/me/profile");
                    req.Headers.Add("Accept", "application/json, text/plain, */*");
                    req.Headers.Add("User-Agent", ProxyTools.UserAgent);
                    req.Headers.Add("Referer", $"{SiteBase}/");
                    req.Headers.Add("Origin", SiteBase);
                    req.Headers.Add("X-Client-Token", cachedKnToken);

                    string cookieHeader = BuildCookieHeader();
                    if (!string.IsNullOrEmpty(cookieHeader))
                        req.Headers.Add("Cookie", cookieHeader);

                    var resp = client.SendAsync(req).Result;
                    return resp.IsSuccessStatusCode;
                }
            }
            catch { }

            return false;
        }

        private static bool TryLogin(string email, string password)
        {
            try
            {
                using (var handler = new HttpClientHandler { UseCookies = false })
                using (var client = new HttpClient(handler))
                {
                    client.Timeout = TimeSpan.FromSeconds(15);
                    var req = new HttpRequestMessage(HttpMethod.Post, $"{SiteBase}/api/auth/login");
                    req.Headers.Add("Accept", "application/json, text/plain, */*");
                    req.Headers.Add("User-Agent", ProxyTools.UserAgent);
                    req.Headers.Add("Referer", $"{SiteBase}/login");
                    req.Headers.Add("Origin", SiteBase);

                    var payload = JsonConvert.SerializeObject(new
                    {
                        email = email,
                        password = password,
                        rememberMe = true
                    });
                    req.Content = new StringContent(payload, Encoding.UTF8, "application/json");

                    var resp = client.SendAsync(req).Result;
                    if (!resp.IsSuccessStatusCode)
                        return false;

                    ExtractCookies(resp);

                    if (!string.IsNullOrEmpty(cachedKnToken))
                    {
                        Ini.SetConfig("KuroMangas", "Token", cachedKnToken, Main.SettingsPath);
                        if (!string.IsNullOrEmpty(cachedKuroSession))
                            Ini.SetConfig("KuroMangas", "Session", cachedKuroSession, Main.SettingsPath);

                        return true;
                    }
                }
            }
            catch { }

            return false;
        }

        private static void ExtractCookies(HttpResponseMessage response)
        {
            if (response.Headers.TryGetValues("Set-Cookie", out var setCookies))
            {
                foreach (var raw in setCookies)
                {
                    var cookiePart = raw.Split(';')[0];
                    var eqIdx = cookiePart.IndexOf('=');
                    if (eqIdx > 0)
                    {
                        string name = cookiePart.Substring(0, eqIdx).Trim();
                        string val = cookiePart.Substring(eqIdx + 1).Trim();

                        if (name == "_kn")
                            cachedKnToken = val;
                        else if (name == "kuro_session")
                            cachedKuroSession = val;
                    }
                }
            }
        }

        private static void LoginViaBrowser()
        {
            var browser = new ChromiumWebBrowser("about:blank");
            try
            {
                browser.Size = new Size(1024, 768);
                browser.WaitInitialize();
                browser.WaitForLoad($"{SiteBase}/login");
                browser.WaitForLoad(15);

                bool isDone = false;
                Func<bool> checkFinish = () =>
                {
                    if (isDone) return true;
                    try
                    {
                        var cookies = browser.GetBrowser().GetCookies();
                        if (cookies != null)
                        {
                            var knCookie = cookies.FirstOrDefault(c => c.Name == "_kn");
                            var sessCookie = cookies.FirstOrDefault(c => c.Name == "kuro_session");

                            if (knCookie != null && !string.IsNullOrEmpty(knCookie.Value))
                            {
                                cachedKnToken = knCookie.Value;
                                if (sessCookie != null)
                                    cachedKuroSession = sessCookie.Value;

                                isDone = true;
                                return true;
                            }
                        }
                    }
                    catch { }
                    return false;
                };

                if (checkFinish())
                    return;

                var popup = new BrowserPopup(browser, checkFinish);
                if (Main.Instance != null && Main.Instance.InvokeRequired)
                {
                    Main.Instance.Invoke(new Action(() => popup.ShowDialog(Main.Instance)));
                }
                else if (Main.Instance != null)
                {
                    popup.ShowDialog(Main.Instance);
                }
                else
                {
                    popup.ShowDialog();
                }

                if (!string.IsNullOrEmpty(cachedKnToken))
                {
                    Ini.SetConfig("KuroMangas", "Token", cachedKnToken, Main.SettingsPath);
                    if (!string.IsNullOrEmpty(cachedKuroSession))
                        Ini.SetConfig("KuroMangas", "Session", cachedKuroSession, Main.SettingsPath);
                }
                else
                {
                    throw new Exception("Falha na autenticação do Kuro Mangás. Faça login na conta para continuar.");
                }
            }
            finally
            {
                browser.Dispose();
            }
        }

        private static string BuildCookieHeader()
        {
            var sb = new StringBuilder();
            if (!string.IsNullOrEmpty(cachedKnToken))
                sb.Append($"_kn={cachedKnToken}; ");
            if (!string.IsNullOrEmpty(cachedKuroSession))
                sb.Append($"kuro_session={cachedKuroSession}; ");

            return sb.ToString().TrimEnd(' ', ';');
        }


        private static string DerivePassword()
        {
            string dateStr = DateTime.UtcNow.ToString("yyyy-MM-dd");
            string md5Input = $"{dateStr}{HostnamePart}{Antibot}";
            using (var md5 = MD5.Create())
            {
                byte[] hash = md5.ComputeHash(Encoding.UTF8.GetBytes(md5Input));
                string sub8 = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant().Substring(0, 8);
                return cachedApiEncryptionKey + sub8;
            }
        }

        private static string DecryptPayload(string vSecure, string dataKey)
        {
            try
            {
                string password = DerivePassword();
                string plainJson = RabbitDecrypt(vSecure, password);

                if (!string.IsNullOrEmpty(dataKey))
                {
                    try
                    {
                        var obj = JObject.Parse(plainJson);
                        if (obj[dataKey] != null)
                            return obj[dataKey].ToString();
                    }
                    catch { }
                }

                return plainJson;
            }
            catch
            {
                try
                {
                    RefreshEncryptionKey();
                    string password = DerivePassword();
                    string plainJson = RabbitDecrypt(vSecure, password);

                    if (!string.IsNullOrEmpty(dataKey))
                    {
                        try
                        {
                            var obj = JObject.Parse(plainJson);
                            if (obj[dataKey] != null)
                                return obj[dataKey].ToString();
                        }
                        catch { }
                    }

                    return plainJson;
                }
                catch
                {
                    return null;
                }
            }
        }

        private static void RefreshEncryptionKey()
        {
            try
            {
                using (var client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(10);
                    client.DefaultRequestHeaders.Add("User-Agent", ProxyTools.UserAgent);
                    string html = client.GetStringAsync(SiteBase).Result;
                    var match = Regex.Match(html, @"src=""(/assets/index-[^""]+\.js)""");
                    if (match.Success)
                    {
                        string jsUrl = SiteBase + match.Groups[1].Value;
                        string js = client.GetStringAsync(jsUrl).Result;
                        var keyMatch = Regex.Match(js, @"VITE_API_ENCRYPTION_KEY\s*:\s*[""']([^""']+)[""']");
                        if (keyMatch.Success)
                        {
                            cachedApiEncryptionKey = keyMatch.Groups[1].Value;
                        }
                    }
                }
            }
            catch { }
        }

        public static string RabbitDecrypt(string ciphertextB64, string password)
        {
            byte[] raw = Convert.FromBase64String(ciphertextB64);
            string header = Encoding.UTF8.GetString(raw, 0, 8);
            if (header != "Salted__")
                throw new InvalidDataException("Invalid OpenSSL format");

            byte[] salt = new byte[8];
            Array.Copy(raw, 8, salt, 0, 8);

            byte[] ciphertext = new byte[raw.Length - 16];
            Array.Copy(raw, 16, ciphertext, 0, ciphertext.Length);

            byte[] passBytes = Encoding.UTF8.GetBytes(password);
            byte[] d1Input = new byte[passBytes.Length + 8];
            Buffer.BlockCopy(passBytes, 0, d1Input, 0, passBytes.Length);
            Buffer.BlockCopy(salt, 0, d1Input, passBytes.Length, 8);

            byte[] d1, d2;
            using (var md5 = MD5.Create())
            {
                d1 = md5.ComputeHash(d1Input);

                byte[] d2Input = new byte[16 + passBytes.Length + 8];
                Buffer.BlockCopy(d1, 0, d2Input, 0, 16);
                Buffer.BlockCopy(passBytes, 0, d2Input, 16, passBytes.Length);
                Buffer.BlockCopy(salt, 0, d2Input, 16 + passBytes.Length, 8);
                d2 = md5.ComputeHash(d2Input);
            }

            uint[] keyWords = new uint[4];
            for (int i = 0; i < 4; i++)
                keyWords[i] = (uint)((d1[i * 4] << 24) | (d1[i * 4 + 1] << 16) | (d1[i * 4 + 2] << 8) | d1[i * 4 + 3]);

            uint[] ivWords = new uint[2];
            for (int i = 0; i < 2; i++)
                ivWords[i] = (uint)((d2[i * 4] << 24) | (d2[i * 4 + 1] << 16) | (d2[i * 4 + 2] << 8) | d2[i * 4 + 3]);

            uint[] M = new uint[4];
            for (int f = 0; f < 4; f++)
                M[f] = SwapEndian(keyWords[f]);

            uint[] X = new uint[]
            {
                M[0], (M[3] << 16 | M[2] >> 16),
                M[1], (M[0] << 16 | M[3] >> 16),
                M[2], (M[1] << 16 | M[0] >> 16),
                M[3], (M[2] << 16 | M[1] >> 16)
            };

            uint[] C = new uint[]
            {
                (M[2] << 16 | M[2] >> 16),
                (M[0] & 0xffff0000 | M[1] & 0x0000ffff),
                (M[3] << 16 | M[3] >> 16),
                (M[1] & 0xffff0000 | M[2] & 0x0000ffff),
                (M[0] << 16 | M[0] >> 16),
                (M[2] & 0xffff0000 | M[3] & 0x0000ffff),
                (M[1] << 16 | M[1] >> 16),
                (M[3] & 0xffff0000 | M[0] & 0x0000ffff)
            };

            uint b = 0;
            uint[] u = new uint[8];
            uint[] h = new uint[8];

            void NextState()
            {
                for (int f = 0; f < 8; f++) h[f] = C[f];
                C[0] = C[0] + 1295307597u + b;
                C[1] = C[1] + 3545052371u + (C[0] < h[0] ? 1u : 0u);
                C[2] = C[2] + 886263092u + (C[1] < h[1] ? 1u : 0u);
                C[3] = C[3] + 1295307597u + (C[2] < h[2] ? 1u : 0u);
                C[4] = C[4] + 3545052371u + (C[3] < h[3] ? 1u : 0u);
                C[5] = C[5] + 886263092u + (C[4] < h[4] ? 1u : 0u);
                C[6] = C[6] + 1295307597u + (C[5] < h[5] ? 1u : 0u);
                C[7] = C[7] + 3545052371u + (C[6] < h[6] ? 1u : 0u);
                b = (C[7] < h[7] ? 1u : 0u);

                for (int f = 0; f < 8; f++)
                {
                    uint j = X[f] + C[f];
                    ulong sq = (ulong)j * j;
                    u[f] = (uint)(sq ^ (sq >> 32));
                }

                X[0] = u[0] + (u[7] << 16 | u[7] >> 16) + (u[6] << 16 | u[6] >> 16);
                X[1] = u[1] + (u[0] << 8 | u[0] >> 24) + u[7];
                X[2] = u[2] + (u[1] << 16 | u[1] >> 16) + (u[0] << 16 | u[0] >> 16);
                X[3] = u[3] + (u[2] << 8 | u[2] >> 24) + u[1];
                X[4] = u[4] + (u[3] << 16 | u[3] >> 16) + (u[2] << 16 | u[2] >> 16);
                X[5] = u[5] + (u[4] << 8 | u[4] >> 24) + u[3];
                X[6] = u[6] + (u[5] << 16 | u[5] >> 16) + (u[4] << 16 | u[4] >> 16);
                X[7] = u[7] + (u[6] << 8 | u[6] >> 24) + u[5];
            }

            for (int f = 0; f < 4; f++) NextState();
            for (int f = 0; f < 8; f++) C[f] ^= X[(f + 4) & 7];

            uint F = SwapEndian(ivWords[0]);
            uint Z = SwapEndian(ivWords[1]);
            uint A = (F >> 16 | Z & 0xffff0000);
            uint bConst = (Z << 16 | F & 0x0000ffff);
            C[0] ^= F; C[1] ^= A; C[2] ^= Z; C[3] ^= bConst;
            C[4] ^= F; C[5] ^= A; C[6] ^= Z; C[7] ^= bConst;
            for (int f = 0; f < 4; f++) NextState();

            int paddedLen = ((ciphertext.Length + 3) / 4) * 4;
            uint[] cipherWords = new uint[paddedLen / 4];
            for (int i = 0; i < cipherWords.Length; i++)
            {
                int offset = i * 4;
                uint word = 0;
                if (offset < ciphertext.Length) word |= ((uint)ciphertext[offset] << 24);
                if (offset + 1 < ciphertext.Length) word |= ((uint)ciphertext[offset + 1] << 16);
                if (offset + 2 < ciphertext.Length) word |= ((uint)ciphertext[offset + 2] << 8);
                if (offset + 3 < ciphertext.Length) word |= (uint)ciphertext[offset + 3];
                cipherWords[i] = word;
            }

            uint[] keyStream = new uint[4];
            for (int block = 0; block < cipherWords.Length; block += 4)
            {
                NextState();
                keyStream[0] = X[0] ^ (X[5] >> 16) ^ (X[3] << 16);
                keyStream[1] = X[2] ^ (X[7] >> 16) ^ (X[5] << 16);
                keyStream[2] = X[4] ^ (X[1] >> 16) ^ (X[7] << 16);
                keyStream[3] = X[6] ^ (X[3] >> 16) ^ (X[1] << 16);
                for (int j = 0; j < 4; j++)
                {
                    keyStream[j] = SwapEndian(keyStream[j]);
                    if (block + j < cipherWords.Length)
                    {
                        cipherWords[block + j] ^= keyStream[j];
                    }
                }
            }

            byte[] plainBytes = new byte[ciphertext.Length];
            for (int i = 0; i < ciphertext.Length; i++)
            {
                int wordIdx = i / 4;
                int byteShift = 24 - (i % 4) * 8;
                plainBytes[i] = (byte)((cipherWords[wordIdx] >> byteShift) & 0xff);
            }

            return Encoding.UTF8.GetString(plainBytes);
        }

        private static uint SwapEndian(uint val)
        {
            return ((val << 8 | val >> 24) & 0x00ff00ff) | ((val << 24 | val >> 8) & 0xff00ff00);
        }

        private static bool IsSupportedHost(Uri uri)
        {
            if (uri == null || string.IsNullOrWhiteSpace(uri.Host))
                return false;

            return SupportedHosts.Any(h => uri.Host.Equals(h, StringComparison.OrdinalIgnoreCase));
        }

        private static int ExtractMangaId(Uri uri)
        {
            if (uri == null) return 0;
            var path = uri.AbsolutePath.Trim('/');
            var parts = path.Split('/');
            if (parts.Length >= 2)
            {
                if (parts[0].Equals("manga", StringComparison.OrdinalIgnoreCase) ||
                    parts[0].Equals("read", StringComparison.OrdinalIgnoreCase) ||
                    parts[0].Equals("ler", StringComparison.OrdinalIgnoreCase))
                {
                    if (int.TryParse(parts[1], out int id) && id > 0)
                        return id;
                }
            }
            return 0;
        }

        private static Uri ResolveSeriesUri(Uri uri)
        {
            int id = ExtractMangaId(uri);
            if (id > 0)
                return new Uri($"{SiteBase}/manga/{id}");
            return uri;
        }

        private class ChapterInfo
        {
            public int InternalId { get; set; }
            public int ChapterId { get; set; }
            public double Numero { get; set; }
            public string Name { get; set; }
            public string Titulo { get; set; }
            public string Url { get; set; }
            public double SortNumber { get; set; }
        }
    }
}
