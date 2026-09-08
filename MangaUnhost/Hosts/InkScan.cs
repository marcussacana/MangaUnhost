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
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;

namespace MangaUnhost.Hosts
{
    public class InkScan : IHost
    {
        private const string SiteBase = "https://inkscann.live";
        private const string ApiBase = "https://api.inkscann.live";
        private const string AnonKey = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6InNqeWJmdnlvem5tdHhtamh5Y29qIiwicm9sZSI6ImFub24iLCJpYXQiOjE3Njk1NTI3MTIsImV4cCI6MjA4NTEyODcxMn0.0nWTir-WVr83QrPoIj8GbSt2Tuu3QZONA_TMzyZ8Ljc";
        private const string DefaultEmail = "dummy.dummy@gmail.com";
        private const string DefaultPassword = "123dummy456";
        private const string TurnstileJsUrl = "https://challenges.cloudflare.com/turnstile/v0/api.js?onload=onloadTurnstileCallback&render=explicit";
        private const string CdnAcervoB = "https://inck2.inkscann.live";
        private const string CdnDefault = "https://cdn.inkscann.live";

        private static readonly string[] SupportedHosts = new[]
        {
            "inkscann.live",
            "www.inkscann.live",
            "inkscan.live",
            "www.inkscan.live"
        };

        private static readonly object AuthLock = new object();
        private static string cachedAccessToken = null;
        private static string cachedRefreshToken = null;
        private static DateTime tokenExpiresAt = DateTime.MinValue;

        private Uri currentSeriesUrl;
        private string currentObraId;
        private string currentPastaS3;
        private string currentSlug;
        private bool currentIsAcervoB;

        private readonly Dictionary<int, ChapterInfo> chapterMap = new Dictionary<int, ChapterInfo>();
        private readonly Dictionary<int, string[]> pageMap = new Dictionary<int, string[]>();

        public NovelChapter DownloadChapter(int ID)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<byte[]> DownloadPages(int ID)
        {
            var chapter = chapterMap.ContainsKey(ID) ? chapterMap[ID] : null;
            var referer = chapter?.Url ?? currentSeriesUrl?.AbsoluteUri ?? SiteBase + "/";

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
                Name = "Ink Scan",
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
                   (HTML.Contains("Ink Scan") || HTML.Contains("inkscann") || HTML.Contains("inkscan"));
        }

        public bool IsValidUri(Uri Uri)
        {
            if (Uri == null || !IsSupportedHost(Uri))
                return false;

            string obraId = ExtractObraId(Uri);
            return !string.IsNullOrEmpty(obraId);
        }

        public ComicInfo LoadUri(Uri Uri)
        {
            chapterMap.Clear();
            pageMap.Clear();

            currentSeriesUrl = ResolveSeriesUri(Uri);
            currentObraId = ExtractObraId(currentSeriesUrl);
            if (string.IsNullOrEmpty(currentObraId))
                throw new Exception($"Não foi possível extrair o ID da obra a partir da URL: {Uri}");

            EnsureAuthenticated();

            var json = ApiRequest($"{ApiBase}/rest/v1/obras?id=eq.{currentObraId}&select=*");
            var arr = JArray.Parse(json);
            if (!arr.Any())
                throw new Exception($"Obra não encontrada para o ID: {currentObraId}");

            var obra = arr[0];
            string title = obra["titulo"]?.ToString() ?? "InkScan Obra";
            string coverUrl = obra["capa_url"]?.ToString();
            currentPastaS3 = obra["pasta_s3"]?.ToString();
            currentSlug = obra["slug"]?.ToString();
            currentIsAcervoB = obra["is_acervo_b"]?.Value<bool>() ?? false;

            byte[] coverData = null;
            if (!string.IsNullOrEmpty(coverUrl))
            {
                coverData = DownloadBinary(coverUrl, currentSeriesUrl.AbsoluteUri);
            }

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
            if (string.IsNullOrEmpty(currentObraId))
                throw new Exception("Obra não carregada.");

            EnsureAuthenticated();

            var json = ApiRequest($"{ApiBase}/rest/v1/capitulos?obra_id=eq.{currentObraId}&select=id,numero,titulo,created_at&order=numero.desc");
            var arr = JArray.Parse(json);

            chapterMap.Clear();
            int id = 0;
            foreach (var item in arr)
            {
                string chapId = item["id"]?.ToString();
                double numero = item["numero"]?.Value<double>() ?? 0;
                string rawTitle = item["titulo"]?.ToString();
                string name = FormatChapterName(numero, rawTitle);
                string chapUrl = $"{SiteBase}/manga/{currentObraId}/chapter/{numero.ToString(CultureInfo.InvariantCulture)}";

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
            var body = JsonConvert.SerializeObject(new { id = chapter.ChapterId });
            var json = ApiRequest($"{ApiBase}/functions/v1/get-chapter", "POST", body);

            var data = JObject.Parse(json);
            string cdnBase = currentIsAcervoB ? CdnAcervoB : CdnDefault;
            string pasta = !string.IsNullOrEmpty(currentPastaS3) ? currentPastaS3 : currentSlug;
            string numStr = chapter.Numero.ToString(CultureInfo.InvariantCulture);

            var pages = new List<string>();

            var arquivos = data["arquivos"] as JArray;
            if (arquivos != null && arquivos.Any())
            {
                foreach (var item in arquivos.OrderBy(x => x["ordem"]?.Value<int>() ?? 0))
                {
                    string filename = item["filename"]?.ToString();
                    int ordem = item["ordem"]?.Value<int>() ?? 0;
                    string file = !string.IsNullOrWhiteSpace(filename) ? filename : $"Pag_{ordem}.webp";
                    pages.Add($"{cdnBase}/{pasta}/Cap_{numStr}/{file}");
                }
            }
            else
            {
                var paginas = data["paginas"] as JArray;
                if (paginas != null && paginas.Any())
                {
                    foreach (var item in paginas)
                    {
                        int pageNum = item.Value<int>();
                        pages.Add($"{cdnBase}/{pasta}/Cap_{numStr}/Pag_{pageNum}.webp");
                    }
                }
                else
                {
                    int total = data["total_paginas"]?.Value<int>() ?? 0;
                    for (int p = 1; p <= total; p++)
                    {
                        pages.Add($"{cdnBase}/{pasta}/Cap_{numStr}/Pag_{p}.webp");
                    }
                }
            }

            var result = pages.ToArray();
            pageMap[ID] = result;
            return result;
        }

        private byte[] DownloadBinary(string url, string referer)
        {
            if (string.IsNullOrWhiteSpace(url))
                return null;

            try
            {
                var data = url.TryDownload(
                    Referer: referer ?? SiteBase + "/",
                    UserAgent: ProxyTools.UserAgent,
                    isImage: true);

                if (data != null && data.Length > 0)
                    return data;

                // Fallback com HttpClient direto e timeout de 30s para imagens grandes de webtoon
                data = DownloadDirect(url, referer);
                if (data != null && data.Length > 0)
                    return data;

                // Fallback: tentar substituir pasta_s3 por slug se divergentes
                if (!string.IsNullOrEmpty(currentPastaS3) && !string.IsNullOrEmpty(currentSlug) && currentPastaS3 != currentSlug)
                {
                    string fallbackUrl = url.Replace($"/{currentPastaS3}/Cap_", $"/{currentSlug}/Cap_");
                    if (fallbackUrl != url)
                    {
                        data = fallbackUrl.TryDownload(
                            Referer: referer ?? SiteBase + "/",
                            UserAgent: ProxyTools.UserAgent,
                            isImage: true);

                        if (data != null && data.Length > 0)
                            return data;

                        data = DownloadDirect(fallbackUrl, referer);
                        if (data != null && data.Length > 0)
                            return data;
                    }
                }

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
                    client.DefaultRequestHeaders.Add("Referer", referer ?? SiteBase + "/");
                    client.DefaultRequestHeaders.Add("User-Agent", ProxyTools.UserAgent);
                    return client.GetByteArrayAsync(url).Result;
                }
            }
            catch
            {
                return null;
            }
        }

        private string ApiRequest(string url, string method = "GET", string jsonBody = null, bool retryAuth = true)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("apikey", AnonKey);
                    if (!string.IsNullOrEmpty(cachedAccessToken))
                    {
                        client.DefaultRequestHeaders.Add("Authorization", "Bearer " + cachedAccessToken);
                    }
                    client.DefaultRequestHeaders.Add("Origin", SiteBase);
                    client.DefaultRequestHeaders.Add("Referer", SiteBase + "/");
                    client.DefaultRequestHeaders.Add("User-Agent", ProxyTools.UserAgent);

                    HttpResponseMessage response;
                    if (method.Equals("POST", StringComparison.OrdinalIgnoreCase))
                    {
                        var content = new StringContent(jsonBody ?? "{}", Encoding.UTF8, "application/json");
                        response = client.PostAsync(url, content).Result;
                    }
                    else
                    {
                        response = client.GetAsync(url).Result;
                    }

                    if ((response.StatusCode == HttpStatusCode.Unauthorized || response.StatusCode == HttpStatusCode.Forbidden) && retryAuth)
                    {
                        EnsureAuthenticated(forceRefresh: true);
                        return ApiRequest(url, method, jsonBody, retryAuth: false);
                    }

                    response.EnsureSuccessStatusCode();
                    return response.Content.ReadAsStringAsync().Result;
                }
            }
            catch (Exception)
            {
                if (retryAuth)
                {
                    EnsureAuthenticated(forceRefresh: true);
                    return ApiRequest(url, method, jsonBody, retryAuth: false);
                }
                throw;
            }
        }

        private static void EnsureAuthenticated(bool forceRefresh = false)
        {
            lock (AuthLock)
            {
                if (!forceRefresh && !string.IsNullOrEmpty(cachedAccessToken) && DateTime.UtcNow < tokenExpiresAt.AddMinutes(-2))
                    return;

                if (!string.IsNullOrEmpty(cachedRefreshToken) && TryRefreshToken())
                    return;

                AuthenticateViaCefSharp();
            }
        }

        private static bool TryRefreshToken()
        {
            try
            {
                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("apikey", AnonKey);
                    client.DefaultRequestHeaders.Add("Origin", SiteBase);
                    client.DefaultRequestHeaders.Add("Referer", SiteBase + "/");
                    var payload = JsonConvert.SerializeObject(new { refresh_token = cachedRefreshToken });
                    var content = new StringContent(payload, Encoding.UTF8, "application/json");

                    var response = client.PostAsync($"{ApiBase}/auth/v1/token?grant_type=refresh_token", content).Result;
                    if (!response.IsSuccessStatusCode)
                        return false;

                    var jsonStr = response.Content.ReadAsStringAsync().Result;
                    var data = JObject.Parse(jsonStr);

                    cachedAccessToken = data["access_token"]?.ToString();
                    cachedRefreshToken = data["refresh_token"]?.ToString();
                    int expiresIn = data["expires_in"]?.Value<int>() ?? 3600;
                    tokenExpiresAt = DateTime.UtcNow.AddSeconds(expiresIn);

                    return !string.IsNullOrEmpty(cachedAccessToken);
                }
            }
            catch
            {
                return false;
            }
        }

        private static void AuthenticateViaCefSharp()
        {
            string email = DefaultEmail;
            string password = DefaultPassword;

            try
            {
                var accounts = AccountTools.LoadAccounts("InkScan");
                if (accounts != null && accounts.Length > 0)
                {
                    var acc = accounts[0];
                    if (!string.IsNullOrEmpty(acc.Email))
                        email = acc.Email;
                    else if (!string.IsNullOrEmpty(acc.Login))
                        email = acc.Login;

                    if (!string.IsNullOrEmpty(acc.Password))
                        password = acc.Password;
                }
            }
            catch { }

            string turnstileJs = null;
            try
            {
                using (var http = new HttpClient())
                {
                    http.DefaultRequestHeaders.Add("User-Agent", ProxyTools.UserAgent);
                    turnstileJs = http.GetStringAsync(TurnstileJsUrl).Result;
                }
            }
            catch { }

            var browser = new ChromiumWebBrowser("about:blank");
            try
            {
                browser.Size = new Size(1365, 768);
                browser.WaitInitialize();
                browser.WaitForLoad($"{SiteBase}/auth");
                browser.WaitForLoad(15);
                ThreadTools.Wait(3000, true);

                if (!string.IsNullOrEmpty(turnstileJs))
                {
                    browser.GetBrowser().MainFrame.ExecuteJavaScriptAsync(turnstileJs);
                }

                string captchaToken = null;
                for (int i = 0; i < 20; i++)
                {
                    ThreadTools.Wait(1000, true);
                    string poll = @"(function() {
                        return (typeof window.turnstile !== 'undefined') ? window.turnstile.getResponse() : null;
                    })()";
                    captchaToken = browser.EvaluateScript<string>(poll);
                    if (!string.IsNullOrWhiteSpace(captchaToken))
                        break;
                }

                if (string.IsNullOrWhiteSpace(captchaToken))
                    throw new Exception("Não foi possível obter o token do Cloudflare Turnstile no InkScan.");

                string loginScript = $@"(async function() {{
                    try {{
                        var apikey = '{AnonKey}';
                        var res = await fetch('{ApiBase}/auth/v1/token?grant_type=password', {{
                            method: 'POST',
                            headers: {{
                                'apikey': apikey,
                                'Authorization': 'Bearer ' + apikey,
                                'Content-Type': 'application/json'
                            }},
                            body: JSON.stringify({{
                                email: '{email}',
                                password: '{password}',
                                gotrue_meta_security: {{
                                    captcha_token: '{captchaToken}'
                                }}
                            }})
                        }});
                        var text = await res.text();
                        return JSON.stringify({{ status: res.status, body: text }});
                    }} catch(e) {{
                        return JSON.stringify({{ error: e.message }});
                    }}
                }})()";

                string loginResult = browser.EvaluateScript<string>(loginScript);
                if (string.IsNullOrWhiteSpace(loginResult))
                    throw new Exception("Falha ao executar script de login no InkScan.");

                var resObj = JObject.Parse(loginResult);
                int status = resObj["status"]?.Value<int>() ?? 0;
                if (status != 200)
                    throw new Exception($"Falha ao autenticar no InkScan (status {status}): {resObj["body"]}");

                var bodyObj = JObject.Parse(resObj["body"]?.ToString() ?? "{}");
                cachedAccessToken = bodyObj["access_token"]?.ToString();
                cachedRefreshToken = bodyObj["refresh_token"]?.ToString();
                int expiresIn = bodyObj["expires_in"]?.Value<int>() ?? 3600;
                tokenExpiresAt = DateTime.UtcNow.AddSeconds(expiresIn);

                if (string.IsNullOrEmpty(cachedAccessToken))
                    throw new Exception("Token de acesso vazio retornado pelo InkScan.");
            }
            finally
            {
                browser.Dispose();
            }
        }

        private static bool IsSupportedHost(Uri uri)
        {
            if (uri == null) return false;
            return SupportedHosts.Any(h => uri.Host.Equals(h, StringComparison.OrdinalIgnoreCase));
        }

        private static string ExtractObraId(Uri uri)
        {
            if (uri == null) return null;
            var path = uri.AbsolutePath.Trim('/');
            var parts = path.Split('/');
            if (parts.Length >= 2 && parts[0].Equals("manga", StringComparison.OrdinalIgnoreCase))
            {
                if (Guid.TryParse(parts[1], out _))
                    return parts[1];
            }
            return null;
        }

        private static Uri ResolveSeriesUri(Uri uri)
        {
            string id = ExtractObraId(uri);
            if (!string.IsNullOrEmpty(id))
                return new Uri($"{SiteBase}/manga/{id}");
            return uri;
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

        private class ChapterInfo
        {
            public int InternalId { get; set; }
            public string ChapterId { get; set; }
            public double Numero { get; set; }
            public string Name { get; set; }
            public string Titulo { get; set; }
            public string Url { get; set; }
            public double SortNumber { get; set; }
        }
    }
}
