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
            foreach (var page in GetChapterPages(ID))
            {
                yield return page.TryDownload(CFData, $"https://{CurrentHost}/obra/{currentBook}/capitulo/{ID}", Headers: Headers);
            }
        }

        public IEnumerable<KeyValuePair<int, string>> EnumChapters()
        {
            return currentBookInfo.capitulos
                .Select(x => new KeyValuePair<int, string>(x.cap_id, x.cap_numero));
        }

        public int GetChapterPageCount(int ID)
        {
            return GetChapterPages(ID).Length;
        }

        private string apiPrefix = null;
        private string[] GetChapterPages(int ID)
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

            var chapterData = JsonConvert.DeserializeObject<APIResult<ChapterData>>(apiData).resultado;
            return chapterData.cap_paginas.Select(x => $"https://{CDN}/scans/{ScanId}/obras/{currentBook}/capitulos/{chapterData.cap_numero}/{x.src}").ToArray();
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
                Version = new Version(1, 1)
            };
        }

        public bool IsValidPage(string HTML, Uri URL)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(HTML);

            var indexNode = doc.SelectSingleNode("//script[contains(@src, '/index-')]");

            if (indexNode != null)
            {
                var scriptUrl = new Uri(URL, indexNode.GetAttributeValue("src", null));
                var scriptData = DownloadString(scriptUrl.AbsoluteUri);

                if (scriptData?.Contains("https://cdn") ?? false)
                {
                    CDN = scriptData.Substring(scriptData.IndexOf("https://cdn")).Substring("://", "\",");
                }
                if (scriptData?.Contains("https://api.") ?? false)
                {
                    API = scriptData.Substring(scriptData.IndexOf("https://api.")).Substring("", "\",");
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
        public ComicInfo LoadUri(Uri Uri)
        {
            currentBook = Uri.LocalPath.Split('/')[2];

            var doc = new HtmlDocument();

            CFData = doc.LoadUrl($"https://{Uri.Host}/obra/" + currentBook);

            CDN = $"storage.{Uri.Host}";
            API = $"https://api.{Uri.Host}";
            CurrentHost = Uri.Host;

            var indexNode = doc.SelectSingleNode("//script[contains(@src, '/index-')]");

            if (indexNode != null) {
                var scriptUrl = new Uri(Uri, indexNode.GetAttributeValue("src", null));
                var scriptData = DownloadString(scriptUrl.AbsoluteUri);

                if (scriptData?.Contains("https://cdn") ?? false) {
                    CDN = scriptData.Substring(scriptData.IndexOf("https://cdn")).Substring("://", "\",");
                }
                if (scriptData?.Contains("https://api.") ?? false)
                {
                    API = scriptData.Substring(scriptData.IndexOf("https://api.")).Substring("", "\",");
                }

                if (apiPrefix == null)
                {
                    var assets = scriptData.Substring("=[\"", "]").Split(',').Select(x=>x.Trim(' ', '"', '\''));

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
                                    break;
                                }
                            }
                            catch { }
                        }
                    }


                }
            }

            var apiInfo = DownloadString($"{API}/scan-info");

            if (apiInfo != null)
            {
                var scanInfo = JsonConvert.DeserializeObject<APIResult<ScanInfo>>(apiInfo);
                if (scanInfo.success)
                {
                    ScanId = scanInfo.resultado.scan_id.ToString();
                }
            }

            var apiData = DownloadString($"{API}/obras/{currentBook}");

            currentBookInfo = JsonConvert.DeserializeObject<APIResult<BookInfo>>(apiData).resultado;

            return new ComicInfo()
            {
                ContentType = ContentType.Comic,
                Cover = $"https://{CDN}/scans/{ScanId}/obras/{currentBook}/{currentBookInfo.obr_imagem}".TryDownload(CFData, Uri.AbsoluteUri, Headers: Headers),
                Title = currentBookInfo.obr_nome,
                Url = Uri,
            };
        }

        private (string Key, string Value)[] Headers => new (string Key, string Value)[]
        {
            ("Origin", $"https://{CurrentHost}"),
            ("scan-id", $"{CurrentHost}")
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
