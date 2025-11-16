using HtmlAgilityPack;
using MangaUnhost.Browser;
using MangaUnhost.Decoders;
using MangaUnhost.Others;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MangaUnhost.Hosts
{
    internal class SussyToons : IHost
    {
        public NovelChapter DownloadChapter(int ID)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<byte[]> DownloadPages(int ID)
        {
            var pagesA = GetChapterPages(ID, false);
            var pagesB = GetChapterPages(ID, true);
            for (var i = 0; i < pagesA.Length; i++)
            {
                yield return pagesA[i].TryDownload(CFData, $"https://{CurrentHost}/obra/{currentBook}/capitulo/{ID}", Headers: Headers) ??
                    pagesB[i].TryDownload(CFData, $"https://{CurrentHost}/obra/{currentBook}/capitulo/{ID}", Headers: Headers);
            }
        }

        public IEnumerable<KeyValuePair<int, string>> EnumChapters()
        {
            return currentBookInfo.capitulos
                .OrderByDescending(x => double.Parse(x.cap_numero.Replace(',', '.'), System.Globalization.CultureInfo.InvariantCulture))
                .Select(x => new KeyValuePair<int, string>(x.cap_id, x.cap_numero));
        }

        public int GetChapterPageCount(int ID)
        {
            return GetChapterPages(ID, false).Length;
        }

        private string apiPrefix = null;
        private string[] GetChapterPages(int ID, bool AltType)
        {

            string apiData;
            if (apiPrefix != null)
            {
                apiData = DownloadString($"{API}/{apiPrefix}/{ID}");
            }
            else
            {
                apiData = DownloadString($"{API}/capitulos/{ID}");
            }

            ChapterData chapterData = JsonConvert.DeserializeObject<APIResult<ChapterData>>(apiData).resultado;
            if (chapterData.cap_id == 0)
                chapterData = JsonConvert.DeserializeObject<ChapterData>(apiData);

            


            if (AltType && CDNRoot != null)
            {
                return chapterData.cap_paginas.Select(x => x.path == null ? $"https://{CDN}/{CDNRoot.Trim('/')}/{x.src.Trim('/')}" : $"https://{CDN}/{x.path.Trim('/')}/{x.src.Trim('/')}").ToArray();
            }
            else
            {
                return chapterData.cap_paginas.Select(x => $"https://{CDN}/scans/{ScanId}/obras/{currentBookInfo.obr_id}/capitulos/{chapterData.cap_numero}/{x.src}").ToArray();
            }
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
                GenericPlugin = true,
                Name = "SussyToons",
                SupportComic = true,
                SupportNovel = false,
                Version = new Version(1, 4)
            };
        }

        public bool IsValidPage(string HTML, Uri URL)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(HTML);

            return GetInfo(URL, doc);
        }

        private bool GetInfo(Uri URL, HtmlDocument doc)
        {
            var indexNode = doc.SelectSingleNode("//script[contains(@src, '/index-')]");

            if (indexNode != null)
            {
                CurrentHost = URL.Host;

                var scriptUrl = new Uri(URL, indexNode.GetAttributeValue("src", null));
                var scriptData = DownloadString(scriptUrl.AbsoluteUri);

                if (scriptData.Contains("https://cdn"))
                {
                    CDN = scriptData.Substring(scriptData.IndexOf("https://cdn")).Substring("://", "\",");
                }
                if (scriptData.Contains("https://api."))
                {
                    API = scriptData.Substring(scriptData.IndexOf("https://api.")).Substring("", "\",");
                }

                if (API == null && scriptData.Contains("=\"https://api"))
                {
                    API = scriptData.Substring(scriptData.IndexOf("=\"https://api") + 2).Substring("", "\"");
                }

                if (scriptData.Contains("manga_\")?r="))
                {
                    CDNRoot = scriptData.Substring("manga_\")?r=", "$");
                    CDNRoot = CDNRoot.Trim('(', '`', ')', '\'', '\"');
                }

                if (apiPrefix == null)
                {
                    var assets = scriptData.Substring("=[\"", "]").Split(',').Select(x => x.Trim(' ', '"', '\''));

                    if (assets != null)
                    {
                        foreach (var asset in assets)
                        {
                            if (!asset.Contains("Page-"))
                                continue;

                            scriptUrl = new Uri(new Uri($"https://{CurrentHost}"), asset);
                            scriptData = DownloadString(scriptUrl.AbsoluteUri);


                            //await T.get(`c9812736812/${o}`)).data.resultado
                            try
                            {
                                if (scriptData?.Contains(").data.resultado") ?? false)
                                {
                                    apiPrefix = scriptData.Substring(0, scriptData.IndexOf(").data.resultado"));
                                    apiPrefix = apiPrefix.Substring(apiPrefix.LastIndexOf("(") + 1);
                                    apiPrefix = apiPrefix.Substring("", "$").Split('/').First().Trim('(', '`', ')', '\'', '\"');
                                    return true;
                                }
                            }
                            catch { }
                        }
                    }

                    if (CDN != null && API != null)
                        return true;
                }
            }

            return false;
        }

        public bool IsValidUri(Uri Uri)
        {
            return Uri.Host.Contains("empreguetes") && Uri.PathAndQuery.Contains("obra/");
        }

        private string currentBook = string.Empty;
        private BookInfo currentBookInfo;
        private CloudflareData? CFData = null;
        private string CurrentHost;
        private string CDN;
        private string API;
        private string ScanId;
        private string ScanIdApi;
        private string CDNRoot;
        public ComicInfo LoadUri(Uri Uri)
        {
            currentBook = Uri.LocalPath.Split('/')[2];

            var doc = new HtmlDocument();

            CFData = doc.LoadUrl($"https://{Uri.Host}/obra/" + currentBook);

            CDN = $"storage.{Uri.Host}";
            CurrentHost = Uri.Host;

            GetInfo(Uri, doc);

            API ??= $"https://api.{Uri.Host}";

            var apiInfo = DownloadString($"{API}/scan-info");

            /* Scan-Id inicial, Ou é o hostname ou 1 se for o site principal
             qhe = () => new URLSearchParams(window.location.search).get('scan_id'),
            Ghe = () => {
                var n;
                const e = qhe();
                if (e) return e;
                if (Po === 'development') return '1';
                const t = typeof window < 'u' ? (n = window == null ? void 0 : window.location) == null ? void 0 : n.hostname : '';
                return t != null &&
                t.includes('sussytoons.wtf') ? '1' : t ||
                '1'
            },
             */

            if (string.IsNullOrWhiteSpace(apiInfo) && API.Contains("sussytoons.wtf"))
            {
                ScanIdApi = "1";
                apiInfo = DownloadString($"{API}/scan-info");
                if (string.IsNullOrWhiteSpace(apiInfo))
                {
                    ScanIdApi = null;
                }
            }

            if (!string.IsNullOrWhiteSpace(apiInfo))
            {
                var scanInfo = JsonConvert.DeserializeObject<APIResult<ScanInfo>>(apiInfo);
                if (scanInfo.success)
                {
                    ScanId = scanInfo.resultado.scan_id.ToString();
                }
            } 
            else
            {
                apiInfo = DownloadString($"{API}/minha-scan");

                if (!string.IsNullOrWhiteSpace(apiInfo))
                {
                    var scanInfo = JsonConvert.DeserializeObject<ScanInfo>(apiInfo);
                    ScanId = scanInfo.scan_id.ToString();
                }
            }

            var apiData = DownloadString($"{API}/obras/{currentBook}");

            currentBookInfo = JsonConvert.DeserializeObject<APIResult<BookInfo>>(apiData).resultado;
            
            if (currentBookInfo.obr_nome == null)
                currentBookInfo = JsonConvert.DeserializeObject<BookInfo>(apiData);

            return new ComicInfo()
                {
                    ContentType = ContentType.Comic,
                    Cover = currentBookInfo.obr_imagem.Contains("/") ?
                    $"https://{CDN}/{currentBookInfo.obr_imagem}".TryDownload(CFData, Uri.AbsoluteUri, Headers: Headers) :
                    $"https://{CDN}/scans/{ScanId}/obras/{currentBookInfo.obr_id}/{currentBookInfo.obr_imagem}".TryDownload(CFData, Uri.AbsoluteUri, Headers: Headers),
                    Title = currentBookInfo.obr_nome,
                    Url = Uri,
                };
        }

        private (string Key, string Value)[] Headers => new (string Key, string Value)[]
        {
            ("Origin", $"https://{CurrentHost}"),
            ("scan-id", $"{ScanIdApi??CurrentHost}")
        };

        private string DownloadString(string url)
        {
            CFData = new CloudflareData() {
                Cookies = CFData?.Cookies,
                UserAgent = CFData?.UserAgent ?? ProxyTools.UserAgent,
                HTML = CFData?.HTML
            };
            return new Uri(url).TryDownloadString(CFData, $"https://{CurrentHost}/", Headers: Headers);
        }

        private struct ChapterData {
            public int cap_id;
            public string cap_numero;
            public List<ChapterPage> cap_paginas;
        }

        private struct ChapterPage
        {
            public string src;
            public string path;
        }

        private struct APIResult<T> where T : struct
        {
            public bool success;
            public string message;
            public int statusCode;
            public T resultado;
        }

        private struct ScanInfo
        {
            public int scan_id;
        }

        private struct BookInfo
        {
            public int obr_id;
            public string obr_nome;
            public string obr_imagem;

            public List<ChapterInfo> capitulos;
        }

        private struct ChapterInfo
        {
            public int cap_id;
            public string cap_numero;
            public bool cap_disponivel;
        }
    }
}
