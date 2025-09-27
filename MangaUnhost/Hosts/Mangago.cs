using CefSharp.DevTools.Page;
using CefSharp.OffScreen;
using HtmlAgilityPack;
using MangaUnhost.Browser;
using MangaUnhost.Decoders;
using MangaUnhost.Others;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MangaUnhost.Hosts
{
    internal class Mangago : IHost
    {
        public NovelChapter DownloadChapter(int ID)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<byte[]> DownloadPages(int ID)
        {
            foreach (var page in GetChapterPages(ID))
            {
                var data = page.TryDownload(cfdata);
                if (data != null)
                    yield return data;
            }

            throw new NotImplementedException();
        }

        Dictionary<int, string[]> chapterPages = new Dictionary<int, string[]>();

        private string[] GetChapterPages(int ID)
        {
            if (chapterPages.ContainsKey(ID))
                return chapterPages[ID];

            var chaps = GetChapters();
            var url = chaps.ElementAt(ID).Value;

            if (url.Contains("pg-"))
                url = url.TrimEnd('/').Substring(0, url.TrimEnd('/').LastIndexOf('/'));

            //get from https://pic1.mangapicgallery.com/js/chapter.js?895
            //with https://de4js.kshift.me/ (sojson v4 deob)
            //have some random decrypt when you request the page, better just emulate the browser

            List<string> pages = new List<string>();

            var status = Main.SubStatus;

            int totalPages = 0;

            try { 
                Main.SubStatus = Main.Language.Crawling;

                using ChromiumWebBrowser browser = new ChromiumWebBrowser("about:blank");
                browser.WaitInitialize();

                do
                {
                    var curUrl = url.TrimEnd('/') + $"/{pages.Count+1}";



                    var chapDoc = new HtmlDocument();

                    HtmlNodeCollection pageNodes = null;

                    while (true)
                    {
                        browser.Load(curUrl);
                        browser.WaitForLoad();

                        ThreadTools.Wait(500);

                        chapDoc.LoadHtml(browser.GetHTML());

                        if (!string.IsNullOrEmpty(chapDoc.DocumentNode.InnerText.Trim())) {
                            pageNodes = chapDoc.SelectNodes("//img[contains(@id, 'page') or contains(@class, 'page')]");

                            if (pageNodes != null && pageNodes.Count > 0)
                                break;
                        }
                    }

                    var pageInfo = chapDoc.SelectSingleNode("//script[contains(., 'total_pages')]").InnerHtml.Substring("total_pages", ",").Trim(' ', '=');
                    totalPages = int.Parse(pageInfo);


                    var newPages = pageNodes.Select(x => x.GetAttributeValue("src", null));

                    var hasNewPages = pages.Distinct().Count() != pages.Concat(newPages).Distinct().Count();

                    pages.AddRange(newPages.Where(x=>!pages.Contains(x)));

                } while (pages.Count < totalPages);
             }
            catch { }
            finally { 
                Main.SubStatus = status;
            }

            return chapterPages[ID] = pages.Distinct().ToArray();
        }

        public IEnumerable<KeyValuePair<int, string>> EnumChapters()
        {
            var chaps = GetChapters();
            for (int i = 0; i < chaps.Count; i++)
            {
                var title = chaps.ElementAt(i).Key;
                var url = chaps.ElementAt(i).Value;
                
                title = cleanChapName(title);

                yield return new KeyValuePair<int, string>(i , DataTools.ForceNumber(title).ToString());
            }
        }

        private string cleanChapName(string name)
        {
            name = name.Replace("Ch.", "").Replace("Chapter", "").Trim().Trim('.');
            return DataTools.ForceNumber(name).ToString();
        }

        public int GetChapterPageCount(int ID)
        {
            return GetChapterPages(ID).Length;
        }

        private Dictionary<string, string> GetChapters()
        {
            var chaps = new Dictionary<string, string>();
            var chapters = doc.SelectNodes("//table[@id='chapter_table']//a") ?? doc.SelectNodes("//table[contains(@class, 'uk-table')]//a");

            foreach (var chapter in chapters)
            {
                var url = chapter.GetAttributeValue("href", "");
                if (!url.Contains("/read") && !url.Contains("/chapter")) continue;
                var title = (chapter.SelectSingleParent("//b") ?? chapter).InnerText.Split(':').First().Trim();
                chaps[title] = url;
            }

            return chaps;
        }

        public IDecoder GetDecoder()
        {
            return new CommonImage();
        }

        public PluginInfo GetPluginInfo()
        {
            return new PluginInfo()
            {
                Name = "Mangago",
                Author = "Marcussacana",
                Version = new Version(1, 0, 0, 0),
                SupportComic = true
            };
        }

        public bool IsValidPage(string HTML, Uri URL)
        {
            throw new NotImplementedException();
        }

        public bool IsValidUri(Uri Uri)
        {
            return Uri.Host.ToLower().Contains("mangago.me") && !Uri.PathAndQuery.Contains("uu");
        }

        public static CloudflareData? cfdata = null;
        HtmlDocument doc = new HtmlDocument();
        public ComicInfo LoadUri(Uri Uri)
        {
            cfdata = doc.LoadUrl(Uri);

            var cover = (doc.SelectSingleNode("//div[contains(@class, 'cover')]//img") ?? 
                doc.SelectSingleNode("//div[@id='information']//img[@class='loading']"))
                .GetAttributeValue("src", "");

            var title = doc
                .SelectSingleNode("//div[contains(@class, 'w-title')]").InnerText.Trim();


            return new ComicInfo()
            {
                ContentType = ContentType.Comic,
                Title = title,
                Cover = cover.TryDownload(cfdata),
                Url = Uri
            };
        }

        private const string decryptScript = "function replacePos(strObj, pos, replacetext) {\r\n    var str = strObj.substr(0, pos) + replacetext + strObj.substring(pos + 1, strObj.length);\r\n    return str;\r\n}\r\n\r\nfunction dorder($str, $key) {\r\n    var $strlen = $str.length;\r\n    for ($j = 3; $j >= 2; $j--)\r\n        for ($i = $strlen - 1;\r\n            ($i - ($key[$j] - '0')) >= 0; $i--) {\r\n            if ($i % 2 != 0) {\r\n                $temp = $str[$i - ($key[$j] - '0')];\r\n                $str = replacePos($str, $i - ($key[$j] - '0'), $str[$i]);\r\n                $str = replacePos($str, $i, $temp);\r\n            }\r\n        }\r\n    for ($j = 1; $j >= 0; $j--)\r\n        for ($i = $strlen - 1;\r\n            ($i - ($key[$j] - '0')) >= 0; $i--) {\r\n            if ($i % 2 != 0) {\r\n                $temp = $str[$i - ($key[$j] - '0')];\r\n                $str = replacePos($str, $i - ($key[$j] - '0'), $str[$i]);\r\n                $str = replacePos($str, $i, $temp);\r\n            }\r\n        }\r\n    return $str;\r\n};\r\nvar _0X2c9a16 = 'BASE64';\r\nvar key = CryptoJS.enc.Hex.parse(\"e11adc3949ba59abbe56e057f20f883e\");\r\nvar iv = CryptoJS.enc.Hex.parse(\"1234567890abcdef1234567890abcdef\");\r\nvar opinion = {\r\n\tiv: iv,\r\n\tpadding: CryptoJS.pad.ZeroPadding\r\n};\r\n_0X2c9a16 = CryptoJS.AES.decrypt(_0X2c9a16, key, opinion);\r\n_0X2c9a16 = _0X2c9a16.toString(CryptoJS.enc.Utf8);\r\nvar $str = _0X2c9a16;\r\nvar $key = $str.charAt(19);\r\nvar $key = $key + $str.charAt(23);\r\nvar $key = $key + $str.charAt(31);\r\nvar $key = $key + $str.charAt(39);\r\n$key = \"abcd\";\r\n$str = _0X2c9a16.slice(0, 19);\r\n$str += _0X2c9a16.slice(20, 23);\r\n$str += _0X2c9a16.slice(24, 31);\r\n$str += _0X2c9a16.slice(32, 39);\r\n$str += _0X2c9a16.slice(40);\r\nvar $strlen = $str.length;\r\nfor ($j = 3; $j >= 2; $j--)\r\n\tfor ($i = $strlen - 1;\r\n\t\t($i - ($key[$j] - '0')) >= 0; $i--) {\r\n\t\tif ($i % 2 != 0) {\r\n\t\t\t$temp = $str[$i - ($key[$j] - '0')];\r\n\t\t\t$str = replacePos($str, $i - ($key[$j] - '0'), $str[$i]);\r\n\t\t\t$str = replacePos($str, $i, $temp);\r\n\t\t}\r\n\t}\r\nfor ($j = 1; $j >= 0; $j--)\r\n\tfor ($i = $strlen - 1;\r\n\t\t($i - ($key[$j] - '0')) >= 0; $i--) {\r\n\t\tif ($i % 2 != 0) {\r\n\t\t\t$temp = $str[$i - ($key[$j] - '0')];\r\n\t\t\t$str = replacePos($str, $i - ($key[$j] - '0'), $str[$i]);\r\n\t\t\t$str = replacePos($str, $i, $temp);\r\n\t\t}\r\n\t}\r\n$str";
    }
}
