using HtmlAgilityPack;
using MangaUnhost.Browser;
using MangaUnhost.Decoders;
using MangaUnhost.Others;
using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;

namespace MangaUnhost.Hosts
{
    internal class Weloma : IHost
    {
        public NovelChapter DownloadChapter(int ID)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<byte[]> DownloadPages(int ID)
        {
            foreach (var Page in GetPages(ID))
            {
                yield return Page.TryDownload(ChapMap[ID].AbsoluteUri);
            }
        }

        Dictionary<int, Uri> ChapMap = new Dictionary<int, Uri>();

        public IEnumerable<KeyValuePair<int, string>> EnumChapters()
        {
            foreach (var Chap in Doc.SelectNodes("//div/ul/a"))
            {
                var Name = Chap.GetAttributeValue("title", string.Empty);
                var UrlStr = Chap.GetAttributeValue("href", string.Empty);
                var Url = new Uri(new Uri("https://weloma.art"), UrlStr);

                var NumName = Name.ToLower().Replace("chap.", "")
                    .Replace("chapter", "")
                    .Replace(".", CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator)
                    .Trim();

                var ID = ChapMap.Count;
                ChapMap[ID] = Url;

                yield return new KeyValuePair<int, string>(ID, NumName);
            }
        }

        public int GetChapterPageCount(int ID)
        {
            return GetPages(ID).Length;
        }

        string[] GetPages(int ID)
        {
            var Chap = new HtmlDocument();
            Chap.LoadUrl(ChapMap[ID], Referer: MangaUrl.AbsoluteUri);


            var Script = Chap.SelectSingleNode("//script[contains(., \'imgsChapload\')]");

            if (Script != null)
            {
                var ChapID = Script.InnerHtml.Substring("(", ",").Trim();
                Chap.LoadUrl("https://rawinu.com/app/manga/controllers/cont.imagesChap.php?cid=" + ChapID, Referer: "https://rawinu.com");
            }

            var Images = Chap.SelectNodes("//img[contains(@class, \'chapter-img\') and contains(@class, \'lazyload\')]");

            List<string> ImgList = new List<string>();
            foreach (var Img in Images)
            {
                var ImgUrl = Img.GetAttributeValue("data-src", string.Empty).Trim();
                ImgList.Add(ImgUrl);
            }

            return ImgList.ToArray();
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
                Name = "Weloma",
                SupportComic = true,
                Version = new Version(1, 0)
            };
        }

        public bool IsValidPage(string HTML, Uri URL)
        {
            throw new NotImplementedException();
        }

        public bool IsValidUri(Uri Uri)
        {
            return Uri.AbsoluteUri.ToLower().Contains("weloma.art/");
        }

        Uri MangaUrl;
        HtmlDocument Doc;

        public ComicInfo LoadUri(Uri Uri)
        {
            MangaUrl = Uri;
            Doc = new HtmlDocument();
            Doc.LoadUrl(Uri);

            var Cover = Doc.SelectSingleNode("//div[contains(@class, \'info-cover\')]/img").GetAttributeValue("src", string.Empty);

            return new ComicInfo()
            {
                ContentType = ContentType.Comic,
                Title = Doc.SelectSingleNode("//ul[@class=\'manga-info\']/h3").InnerText.Replace("- RAW", ""),
                Cover = Cover.TryDownload(Uri.AbsoluteUri),
                Url = Uri
            };
        }
    }
}
