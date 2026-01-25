using CefSharp;
using CefSharp.OffScreen;
using MangaUnhost.Browser;
using MangaUnhost.Decoders;
using MangaUnhost.Others;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Threading;
using System.Web;

namespace MangaUnhost.Hosts
{
    internal class Yomu : IHost
    {
        Dictionary<string, string> entries = null;
        Dictionary<int, string> chapMap = new Dictionary<int, string>();
        Dictionary<int, List<string>> chapCache = new Dictionary<int, List<string>>();

        public NovelChapter DownloadChapter(int ID)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<byte[]> DownloadPages(int ID)
        {
            foreach (var page in GetPages(ID)) {
                yield return page.TryDownload(Referer: $"https://{currentUrl.Host}", ProxyTools.UserAgent, Retries: 6);
             }
        }

        public IEnumerable<KeyValuePair<int, string>> EnumChapters()
        {
            foreach (var chapter in GetChapters().OrderByDescending(x=> double.Parse(x.Key)))
            {
                int id = chapMap.Count;
                chapMap[id] = chapter.Value;

                yield return new KeyValuePair<int, string>(id, chapter.Key);
            }
        }

        public List<string> GetPages(int id)
        {
            if (chapCache.ContainsKey(id))
                return chapCache[id];

            browser.WaitForLoad(chapMap[id]);
            while (true) {
                try { 
                    ThreadTools.Wait(6000, true);
                    var doc = browser.GetDocument();

                    var pages = new List<string>();
                    foreach (var page in doc.SelectNodes("//*[contains(@class, 'chapter-reading-page')]//img"))
                    {
                        var urlPath = page.GetAttributeValue("src", null);
                        pages.Add(new Uri(new Uri($"https://yomu.com.br"), urlPath).AbsoluteUri);
                    }

                    if (pages.Count == 0)
                        continue;

                    return chapCache[id] = pages;
                } catch (Exception ex) { }
            }
        }
        public Dictionary<string, string> GetChapters()
        {
            if (entries != null)
                return entries;


            var readBase = currentUrl.AbsoluteUri.Replace("/obra/", "/ler/").TrimEnd('/') + "/";

            browser.WaitForLoad(readBase + "1");
            ThreadTools.Wait(5000, true);

            while (true)
            {
                try
                {
                    var pos = browser.GetBounds("//button[contains(@id, 'radix-«r5»')]");
                    browser.ExecuteClick(pos);
                    ThreadTools.Wait(500, true);
                    break;
                }
                catch
                {
                    ThreadTools.Wait(1000, true);
                }
            }


            var doc = browser.GetDocument();
            var chaplist = doc.SelectNodes("//*[contains(@class, 'text-xs text-muted-foreground')]");

            entries = new Dictionary<string, string>();
            foreach (var chapter in chaplist)
            {
                if (!int.TryParse(chapter.InnerText, out _)) continue;
                entries.Add(chapter.InnerText, readBase + chapter.InnerText);
            }

            return entries;
        }

        public void ExpandChapters()
        {
            //div[@class='text-center pt-4 sm:pt-6']/button
        }

        public int GetChapterPageCount(int ID)
        {
            return GetPages(ID).Count;
        }

        public IDecoder GetDecoder()
        {
            return new CommonImage();
        }

        public PluginInfo GetPluginInfo()
        {
            return new PluginInfo()
            {
                Name = "Yomu",
                Author = "Marcussacana",
                SupportComic = true,
                Version = new Version(1, 1)
            };
        }

        public bool IsValidPage(string HTML, Uri URL)
        {
            throw new NotImplementedException();
        }

        public bool IsValidUri(Uri Uri)
        {
            return Uri.Host.Contains("yomu") && Uri.PathAndQuery.Contains("obra");
        }

        Uri currentUrl;
        static ChromiumWebBrowser browser;

        public ComicInfo LoadUri(Uri Uri)
        {
            currentUrl = Uri;

            if (browser == null)
            {
                browser = new ChromiumWebBrowser(Uri.AbsoluteUri);
                browser.Size = new System.Drawing.Size(1280, 720);
                browser.WaitForLoad();
            }
            else
                browser.WaitForLoad(Uri.AbsoluteUri);

            ThreadTools.Wait(3000, true);
            //browser.ShowDevTools();
            Login();

            int tries = 10;
            while (tries-- > 0)
            {
                try
                {
                    var doc = browser.GetDocument();

                    var coverNode = doc.SelectSingleNode("//img[contains(@class, 'w-full h-full object-cover scale-')]");
                    var titleNode = doc.SelectSingleNode("//h1[contains(@class, 'text-2xl sm:text-3xl md:text-4xl')]");

                    var coverUrl = coverNode.GetAttributeValue("src", null);
                    var title = titleNode.InnerText;


                    return new ComicInfo()
                    {
                        Title = title,
                        Cover = new Uri(coverUrl).TryDownload(Referer: $"https://{currentUrl.Host}", ProxyTools.UserAgent),
                        Url = currentUrl,
                        ContentType = ContentType.Comic
                    };
                }
                catch (Exception ex){ 
                    ThreadTools.Wait(1000, true);
                }
            }

            throw new Exception("Failed to Load");
        }

        public void Login()
        {
            var retUrl = browser.GetCurrentUrl();

            if (retUrl.Contains("redirect="))
                retUrl = retUrl.Substring("redirect=");
            else
                retUrl = HttpUtility.UrlEncode("/" + retUrl.Substring("//").Substring("/"));

            browser.WaitForLoad("https://yomu.com.br/auth/login?callbackUrl=" + retUrl);

            var doc = browser.GetDocument();
            if (doc.SelectSingleNode("//*[@for='email']") == null)
                return;

            while (true)
            {
                try
                {
                    var pos = browser.GetBounds("//*[@for='remember']/..//input");
                    browser.TypeInInput("//*[@for='email']/../div/input".CreateTargetSelectorFromXPATH(), "anon@anon.com", true);
                    browser.TypeInInput("//*[@for='password']/../div/input".CreateTargetSelectorFromXPATH(), "123anon456", true);
                    browser.ExecuteClick(pos);

                    pos = browser.GetBounds("//button[contains(@class, 'from-orange')]");
                    browser.ExecuteClick(pos);


                    while (browser.GetCurrentUrl().Contains("auth/login"))
                        ThreadTools.Wait(500);

                    browser.WaitForLoad();

                    doc = browser.GetDocument();
                    if (doc.SelectSingleNode("//*[@for='email']") == null)
                        return;

                    throw new Exception("Login failed");
                }
                catch { 
                    ThreadTools.Wait(5000, true);
                }
            }
        }
    }
}
