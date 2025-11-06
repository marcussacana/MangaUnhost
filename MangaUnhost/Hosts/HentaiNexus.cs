using CefSharp;
using HtmlAgilityPack;
using MangaUnhost.Browser;
using MangaUnhost.Others;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace MangaUnhost.Hosts {
    class HentaiNexus : IHost {
        string CurrentUrl;
        HtmlDocument Document;

        public NovelChapter DownloadChapter(int ID) {
            throw new NotImplementedException();
        }

        public IEnumerable<byte[]> DownloadPages(int ID) {
            foreach (var PageUrl in GetChapterPages(ID)) {
                yield return PageUrl.TryDownload();
            }
        }

        public IEnumerable<KeyValuePair<int, string>> EnumChapters() {
            yield return new KeyValuePair<int, string>(0, "Oneshot");
        }


        public int GetChapterPageCount(int ID) {
            return GetChapterPages(ID).Length;
        }

        private string[] GetChapterPages(int ID) {
            var Document = GetChapterHtml(ID);

            List<string> Pages = new List<string>();
            foreach (var Node in Document.SelectNodes("//div[contains(@class, 'is-one-fifth-desktop')]//a")) {
                var doc = new HtmlDocument();
                doc.LoadUrl(new Uri(new Uri(CurrentUrl), Node.GetAttributeValue("href", "")));
                var node = doc.SelectSingleNode("//script[contains(., 'initReader(')]");

                if (node == null)
                    continue;

                var nodeData = node.InnerHtml.Substring("initReader(\"", "\",");

                var jsonData = DecryptJson(nodeData);

                Entry[] entries = JsonConvert.DeserializeObject<Entry[]>(jsonData);

                Pages.AddRange(entries.Where(x=>x.type == "image").Select(x=>x.image));
                break;
            }

            return Pages.ToArray();
        }


        //from https://hentainexus.com/static/js/reader.min.js?r=23
        const string decryptScript = @"function decrypt(r){var o,$,e,t,a,f,h,n,C,c=""hentainexus.com"".split(""""),d=Math.min(c.length,64),i=atob(r).split("""");for(o=0;o<d;o++)i[o]=String.fromCharCode(i[o].charCodeAt(0)^c[o].charCodeAt(0));i=i.join("""");var o,_,l=[],A=[];for(o=2;A.length<16;++o)if(!l[o])for(A.push(o),_=o<<1;_<=256;_+=o)l[_]=!0;var m=0;for(o=0;o<64;o++){m^=i.charCodeAt(o);for(var _=0;_<8;_++)m=1&m?m>>>1^12:m>>>1}for(m&=7,o=[],$=0,t="""",a=0;a<256;a++)o[a]=a;for(a=0;a<256;a++)$=($+o[a]+i.charCodeAt(a%64))%256,e=o[a],o[a]=o[$],o[$]=e;for(C=A[m],h=0,n=0,a=0,$=0,f=0;f+64<i.length;f++)$=(n+o[($+o[a=(a+C)%256])%256])%256,n=(n+a+o[a])%256,e=o[a],o[a]=o[$],o[$]=e,h=o[($+o[(a+o[(h+n)%256])%256])%256],t+=String.fromCharCode(i.charCodeAt(f+64)^h);return t}";

        private string DecryptJson(string Data) {
            JSTools.EvaluateScript(decryptScript);
            return JSTools.EvaluateScript<string>($"decrypt(`{Data}`);");
        }
        private HtmlDocument GetChapterHtml(int ID) {
            return Document;
        }
        public IDecoder GetDecoder() {
            return new Decoders.CommonImage();
        }

        public PluginInfo GetPluginInfo() {
            return new PluginInfo() {
                Author = "Marcussacana",
                Name = "HentaiNexus",
                SupportComic = true,
                SupportNovel = false,
                Version = new Version(1, 0)
            };
        }

        public bool IsValidUri(Uri Uri) {
            string URL = Uri.AbsoluteUri.ToLower();
            return URL.ToLower().Contains("hentainexus.com") && URL.Substring(URL.IndexOf(".com")).Split('/').Length >= 2;
        }

        public ComicInfo LoadUri(Uri Uri) {
            CurrentUrl = Uri.AbsoluteUri.TrimEnd('/');

            Document = new HtmlDocument();
            Document.LoadUrl(Uri);

            ComicInfo Info = new ComicInfo();

            Info.Title = HttpUtility.HtmlDecode(Document.SelectSingleNode("//h1[@class=\"title\"]").InnerText);
            Info.Cover = Document
                .SelectSingleNode("//div[contains(@class, 'is-one-third-desktop')]//img")
                .GetAttributeValue("src", "").TryDownload();
            Info.ContentType = ContentType.Comic;

            return Info;
        }
        public bool IsValidPage(string HTML, Uri URL) => false;

        public struct Entry
        {
            public string image;
            public string label;
            public string type;
        }
    }
}
