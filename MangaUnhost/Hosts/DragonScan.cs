using CefSharp.DevTools.Page;
using HtmlAgilityPack;
using MangaUnhost.Browser;
using MangaUnhost.Decoders;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace MangaUnhost.Hosts
{
    internal class DragonScan : IHost
    {
        public NovelChapter DownloadChapter(int ID)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<byte[]> DownloadPages(int ID)
        {
            List<byte[]> Chapter = null;

            if (ChapData.ContainsKey(ID))
            {
                Chapter = ChapData[ID];
                ChapData.Remove(ID);
            }
            else
            {
                Chapter = GetChapterPages(ID);
                ChapData.Remove(ID);
            }

            return Chapter;
        }

        Dictionary<int, string> ChapMap = new Dictionary<int, string>();
        public IEnumerable<KeyValuePair<int, string>> EnumChapters()
        {
            foreach (var Chap in doc.SelectNodes("//*[@class='capitulos__lista']/a")) {
                var chapName = Chap.SelectSingleParent("//span[@class='numero__capitulo']").InnerText;

                chapName = chapName.Replace("Capítulo", "");
                chapName = chapName.Replace("Cap", "").Trim();

                var Url = new Uri (currentUrl, Chap.GetAttributeValue("href", null)).AbsoluteUri;

                yield return new KeyValuePair<int, string>(ChapMap.Count, chapName);
                ChapMap[ChapMap.Count] = Url;
            }
        }

        Dictionary<int, List<byte[]>> ChapData = new Dictionary<int, List<byte[]>>();

        private List<byte[]> GetChapterPages(int ID)
        {
            var currentUrl = new Uri(ChapMap[ID]);
            var doc = new HtmlDocument();
            cfdata = doc.LoadUrl(currentUrl, cfdata);

            var Script = doc.SelectSingleNode("//script[contains(.,'urls')]").InnerHtml;

            var Result = JSTools.EvaluateScript<string[]>(Script + "urls;");

            List<byte[]> Pages = new List<byte[]>();

            foreach (var url in Result) {
                var finalUrl = new Uri(currentUrl, url);

                var oriData = finalUrl.TryDownload(cfdata, Referer: currentUrl.AbsoluteUri);

                if (!finalUrl.AbsolutePath.EndsWith(".zip", StringComparison.InvariantCultureIgnoreCase)){
                    Pages.Add(oriData);
                    continue;
                }

                using (var zipStream = new MemoryStream(oriData))
                using (var zip = new ZipArchive(zipStream, ZipArchiveMode.Read)){
                    var imgs = zip.Entries.Where(x => !x.Name.EndsWith(".s"));
                    foreach (var img in imgs)
                    {
                        using (var buff = new MemoryStream())
                        using (var Strm = img.Open()){
                            Strm.CopyTo(buff);

                            Pages.Add(buff.ToArray());
                        }
                    }
                }
            }

            ChapData[ID] = Pages;

            return Pages;
        }


        public int GetChapterPageCount(int ID)
        {
            return GetChapterPages(ID).Count;
        }

        public IDecoder GetDecoder()
        {
            return new CommonImage();
        }

        public PluginInfo GetPluginInfo()
        {
            return new PluginInfo()
            {
                Name = "DragonScan",
                GenericPlugin = false,
                SupportComic = true,
                SupportNovel = false,
                Author = "Marcussacana",
                Version = new Version(1, 0)
            };
        }

        public bool IsValidPage(string HTML, Uri URL)
        {
            throw new NotImplementedException();
        }

        public bool IsValidUri(Uri Uri)
        {
            return Uri.Host.Contains("rfdragonscan");
        }

        CloudflareData? cfdata = null;
        HtmlDocument doc = new HtmlDocument();
        Uri currentUrl;
        public ComicInfo LoadUri(Uri Uri)
        {
            currentUrl = Uri;

            cfdata = doc.LoadUrl(Uri);

            var cover = new Uri(Uri, doc.SelectSingleNode("//img[@class='sumario__img']").GetAttributeValue("src", null)).TryDownload(cfdata);

            if (Main.IsAvif(cover))
                cover = Main.DecodeAvif(cover);

            return new ComicInfo
            {
                Title = doc.SelectSingleNode("//h1[@class='desc__titulo__comic']").InnerText.Trim(),
                Cover = cover,
                ContentType = ContentType.Comic,
                Url = Uri
            };
        }
    }
}
