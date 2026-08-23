using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using CefSharp;
using CefSharp.OffScreen;
using MangaUnhost.Browser;
using MangaUnhost.Decoders;
using MangaUnhost.Others;
using Newtonsoft.Json.Linq;

namespace MangaUnhost.Hosts
{
    internal class ComixTo : IHost
    {
        private ChromiumWebBrowser browser;
        private Uri CurrentUrl;
        private string MangaHid;
        private Dictionary<int, string> ChapterMap = new Dictionary<int, string>();
        private Dictionary<int, List<string>> PagesMap = new Dictionary<int, List<string>>();

        private void EnsureBrowser()
        {
            if (browser != null) return;
            browser = new ChromiumWebBrowser("about:blank");
            browser.WaitInitialize();
            browser.EarlyInjection("window.__originalToDataURL = HTMLCanvasElement.prototype.toDataURL; window.__originalCreateElement = Document.prototype.createElement;");
        }

        public ComicInfo LoadUri(Uri Uri)
        {
            CurrentUrl = Uri;
            MangaHid = Regex.Match(Uri.AbsolutePath, @"/title/([^/-]+)").Groups[1].Value;

            EnsureBrowser();
            browser.WaitForLoad(Uri.AbsoluteUri);

            // Sem isso, a pagina do desafio do Cloudflare ("Um momento...") podia ficar so
            // 3s pra resolver e a leitura seguinte pegava a pagina de verificacao vazia em
            // vez do conteudo real, fazendo a obra "nao carregar" (titulo/capa/capitulos
            // vazios sem nenhum erro).
            if (browser.IsCloudflareTriggered())
            {
                browser.BypassCloudflare();
                browser.WaitForLoad(Uri.AbsoluteUri);
            }

            ThreadTools.Wait(3000, true);

            var doc = browser.GetDocument();

            string title = null;
            string coverUrl = null;

            // A pagina embute os dados da obra em JSON (React Query cache); ler dali evita
            // pegar por engano o <h1> ou <img src=static.comix.to> de uma obra "recomendada".
            // Acha a entrada pelo formato dos dados (hid+title+poster), não pelo nome da chave
            // de cache (ex.: ["manga","detail","hid"]), que e' um detalhe interno e pode mudar.
            var dataNode = doc.SelectSingleNode("//script[@id='initial-data']");
            if (dataNode != null)
            {
                try
                {
                    var data = JObject.Parse(dataNode.InnerText);
                    var detail = (data["queries"] as JObject)?.Properties()
                        .Select(p => p.Value as JObject)
                        .FirstOrDefault(v => v?["hid"]?.ToString() == MangaHid && v["title"] != null && v["poster"] != null);

                    title = detail?["title"]?.ToString();
                    coverUrl = detail?["poster"]?["large"]?.ToString() ?? detail?["poster"]?["medium"]?.ToString();
                }
                catch { }
            }

            if (string.IsNullOrWhiteSpace(title))
            {
                var titleNode = doc.SelectSingleNode("//h1");
                title = titleNode != null ? titleNode.InnerText.Trim() : "Unknown";
            }

            if (string.IsNullOrWhiteSpace(coverUrl))
            {
                var coverNode = doc.SelectSingleNode("//img[contains(@src, 'static.comix.to')]");
                coverUrl = coverNode != null ? coverNode.GetAttributeValue("src", "") : "";
            }

            byte[] cover = null;
            if (!string.IsNullOrEmpty(coverUrl))
                cover = coverUrl.TryDownload(Referer: Uri.AbsoluteUri, UserAgent: browser.GetUserAgent(), Cookie: browser.GetCookies().ToContainer());

            return new ComicInfo
            {
                Title = title,
                Cover = cover,
                ContentType = ContentType.Comic,
                Url = Uri
            };
        }

        public IEnumerable<KeyValuePair<int, string>> EnumChapters()
        {
            EnsureBrowser();

            if (browser.Address != CurrentUrl.AbsoluteUri)
            {
                browser.WaitForLoad(CurrentUrl.AbsoluteUri);
                ThreadTools.Wait(3000, true);
            }

            int chapterId = 0;

            while (true)
            {
                ThreadTools.Wait(2000, true);

                var jsGetChapters = @"
                    JSON.stringify(
                        Array.from(document.querySelectorAll('a'))
                             .filter(a => /\/title\/[^/]+\/\d+-chapter/.test(a.href))
                             .map(a => {
                                 let title = a.innerText.trim();
                                 let m = a.href.match(/chapter-([\d.-]+)/i);
                                 let num = m ? m[1] : '';
                                 return {url: a.href, title: title, num: num};
                             })
                    )
                ";

                var chaptersJson = browser.EvaluateScript<string>(jsGetChapters);

                if (!string.IsNullOrEmpty(chaptersJson) && chaptersJson != "null")
                {
                    var chaptersData = JArray.Parse(chaptersJson);
                    foreach (var item in chaptersData)
                    {
                        string url = item["url"]?.ToString();
                        string name = item["title"]?.ToString();
                        string num = item["num"]?.ToString();

                        if (!string.IsNullOrEmpty(num))
                            name = num;

                        if (string.IsNullOrEmpty(url)) continue;
                        if (ChapterMap.Values.Contains(url)) continue;

                        ChapterMap[chapterId] = url;
                        yield return new KeyValuePair<int, string>(chapterId, name);
                        chapterId++;
                    }
                }

                var jsClickNext = @"
                    (function() {
                        var active = document.querySelector('.npager__num.is-active');
                        if (!active) return false;
                        var next = active.nextElementSibling;
                        if (next && next.classList.contains('npager__num')) {
                            next.click();
                            return true;
                        }
                        return false;
                    })()
                ";

                var clickedNext = browser.EvaluateScript<bool>(jsClickNext);
                if (!clickedNext) break;

                ThreadTools.Wait(3000, true);
            }
        }

        public int GetChapterPageCount(int ID)
        {
            return GetChapterPages(ID).Count;
        }

        public IEnumerable<byte[]> DownloadPages(int ID)
        {
            foreach (var url in GetChapterPages(ID))
            {
                if (url.StartsWith("data:"))
                {
                    var commaIdx = url.IndexOf(',');
                    if (commaIdx != -1)
                    {
                        var base64 = url.Substring(commaIdx + 1);
                        yield return Convert.FromBase64String(base64);
                        continue;
                    }
                }
                yield return url.TryDownload(Referer: ChapterMap[ID], UserAgent: browser.GetUserAgent(), Cookie: browser.GetCookies().ToContainer());
            }
        }

        private List<string> GetChapterPages(int ID)
        {
            EnsureBrowser();
            var chapUrl = ChapterMap[ID];

            if (PagesMap.ContainsKey(ID))
                return PagesMap[ID];

            browser.WaitForLoad(chapUrl);
            ThreadTools.Wait(3000, true);

            var jsAccumulateImages = @"
                (async function() {
                    let pagesDict = {};
                    let unknownPages = [];
                    
                    let cleanToDataURL = window.__originalToDataURL;
                    if (!cleanToDataURL) {
                        try {
                            let createEl = window.__originalCreateElement || document.createElement;
                            let ifr = createEl.call(document, 'iframe');
                            ifr.style.display = 'none';
                            document.body.appendChild(ifr);
                            cleanToDataURL = ifr.contentWindow.HTMLCanvasElement.prototype.toDataURL;
                            document.body.removeChild(ifr);
                        } catch (e) {
                            cleanToDataURL = HTMLCanvasElement.prototype.toDataURL;
                        }
                    }

                    function pageNumberOf(pageContainer) {
                        let dataPage = pageContainer.getAttribute('data-page');
                        if (dataPage !== null) return parseInt(dataPage);
                        let label = pageContainer.getAttribute('aria-label');
                        if (label) {
                            let m = label.match(/Page\s+(\d+)/i);
                            if (m) return parseInt(m[1]);
                        }
                        return -1;
                    }

                    // Retorna true se conseguiu capturar a imagem dessa pagina (ou ja tinha).
                    function collectOne(pageContainer) {
                        let pageNum = pageNumberOf(pageContainer);
                        if (pageNum !== -1 && pageNum in pagesDict) return true;

                        let img = pageContainer.querySelector('img');
                        if (img) {
                            let src = img.src || img.getAttribute('data-src') || img.getAttribute('data-lazy-src');
                            if (src && src.startsWith('http') &&
                                !src.includes('avatar') &&
                                !src.includes('logo') &&
                                !src.includes('favicon') &&
                                !src.includes('@280') &&
                                !src.includes('comix.to/assets') &&
                                !src.includes('google.com') &&
                                !src.includes('cloudflare') &&
                                !src.includes('static.comix.to')) {

                                if (pageNum === -1) {
                                    let alt = img.getAttribute('alt') || '';
                                    let m = alt.match(/Page\s+(\d+)/i);
                                    if (m) pageNum = parseInt(m[1]);
                                }

                                if (pageNum !== -1) {
                                    pagesDict[pageNum] = src;
                                } else if (!unknownPages.includes(src)) {
                                    unknownPages.push(src);
                                }
                                return true;
                            }
                        }

                        let canvas = pageContainer.querySelector('canvas');
                        if (canvas && pageNum !== -1) {
                            try {
                                let dataUrl = cleanToDataURL ? cleanToDataURL.call(canvas, 'image/png') : canvas.toDataURL('image/png');
                                if (dataUrl && dataUrl.startsWith('data:image')) {
                                    pagesDict[pageNum] = dataUrl;
                                    return true;
                                }
                            } catch (e) {
                                // Ignore tainted canvas errors
                            }
                        }

                        return false;
                    }

                    // Force remove lazy loading attributes just in case
                    document.querySelectorAll('img[loading]').forEach(img => img.removeAttribute('loading'));

                    let allPages = Array.from(document.querySelectorAll('.rpage-page'));
                    let total = allPages.length;

                    // Passada inicial: percorre a tira toda uma vez pra disparar o lazy-load de cada pagina.
                    for (const p of allPages) {
                        p.scrollIntoView({ block: 'center' });
                        await new Promise(r => setTimeout(r, 180));
                        collectOne(p);
                    }

                    // Fecha o que faltou: repesca SO as paginas ainda sem imagem, com mais tempo a
                    // cada rodada, ate bater o total ou esgotar as tentativas. Uma pagina que so
                    // carrega devagar (rede/lazy-load atrasado) nao pode ficar faltando no capitulo.
                    for (let attempt = 0; attempt < 8; attempt++) {
                        let missing = allPages.filter(p => {
                            let n = pageNumberOf(p);
                            return n === -1 || !(n in pagesDict);
                        });

                        if (missing.length === 0) break;

                        for (const p of missing) {
                            p.scrollIntoView({ block: 'center' });
                            await new Promise(r => setTimeout(r, 500 + attempt * 300));
                            collectOne(p);
                        }
                    }

                    let sortedKeys = Object.keys(pagesDict).map(Number).sort((a,b) => a - b);
                    let finalUrls = sortedKeys.map(k => pagesDict[k]).concat(unknownPages);
                    return JSON.stringify({ pages: finalUrls, total: total });
                })();
            ";

            // Timeout de verdade: sem isso, se o callback nativo do CefSharp nunca disparar
            // (script async rodando minutos por causa da repescagem de paginas), a Task fica
            // presa em WaitingForActivation e o polling abaixo gira pra sempre (softlock).
            var resultJson = browser.EvaluateScriptAsync<string>(jsAccumulateImages).RunInBackground(300);

            var result = new List<string>();
            int expectedTotal = 0;
            if (!string.IsNullOrEmpty(resultJson) && resultJson != "null")
            {
                var parsed = JObject.Parse(resultJson);
                expectedTotal = parsed["total"]?.Value<int>() ?? 0;

                foreach (var img in (JArray)parsed["pages"])
                    result.Add(img.ToString());
            }

            if (expectedTotal > 0 && result.Count < expectedTotal)
                throw new Exception($"Failed to load all chapter pages from Comix.to. Expected {expectedTotal}, got {result.Count}.");

            return PagesMap[ID] = result;
        }

        public IDecoder GetDecoder()
        {
            return new CommonImage();
        }

        public PluginInfo GetPluginInfo()
        {
            return new PluginInfo()
            {
                Name = "ComixTo",
                Author = "Marcussacana",
                SupportComic = true,
                SupportNovel = false,
                GenericPlugin = false,
                Version = new Version(1, 2, 0)
            };
        }

        public bool IsValidPage(string HTML, Uri URL)
        {
            return IsValidUri(URL) && HTML.Contains("comix.to");
        }

        public bool IsValidUri(Uri Uri)
        {
            return Uri.Host.Contains("comix.to") && Uri.AbsolutePath.Contains("/title/");
        }

        public NovelChapter DownloadChapter(int ID)
        {
            throw new NotImplementedException();
        }
    }
}
