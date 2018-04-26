using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MangaUnhost.Host {
    internal class Mangahost : IHost {
        public string HostName { get {
                return "MangaHost";
            }
        }
        public string DemoUrl {
            get {
                return "https://mangahostbr.com/manga/hadi-girl-mh23848";
            }
        }

        string HTML;

        public bool IsValidLink(string URL) {
            URL = URL.ToLower();
            return Uri.IsWellFormedUriString(URL, UriKind.Absolute) && (URL.Contains("mangahost") || URL.Contains("mangashost"));           
        }

        public void Initialize(string URL, out string Name, out string Page) {
            if (!IsValidLink(URL))
                throw new Exception();

            int Index = URL.IndexOf("/manga/");
            string Prefix = string.Empty;
            if (Index < 0)
                throw new Exception();

            Prefix = URL.Substring(0, Index);
            URL = URL.Substring(Index, URL.Length - Index);
            Name = GetName(URL.Split('/')[2]);
            Page = Prefix + "/manga/" + URL.Split('/')[2];
        }

        public void LoadPage(string URL) {
            HTML = Main.Download(URL, Encoding.UTF8);
        }

        public string GetPosterUrl() {
            string Element = Main.GetElementsByClasses(HTML, 0, "pull-left", "thumbnail")[0];
            return Main.GetElementAttribute(Element, "src");
        }

        public string GetName(string CodedName) {
            string ResultName = Main.GetRawNameFromUrlFolder(CodedName);
            string[] words = ResultName.Split(' ');

            int ignore;
            if (words[words.Length - 1].ToLower().StartsWith("mh") && int.TryParse(words[words.Length - 1].ToLower().Replace("mh", ""), out ignore)) {
                int indexof = ResultName.IndexOf(words[words.Length - 1]);
                ResultName = ResultName.Substring(0, indexof);
            }
            return ResultName.Trim();
        }
        
        public string GetFullName() {
            string[] PossibleTitles = Main.GetElementsByContent(HTML, "| Mang");
            if (PossibleTitles != null && PossibleTitles.Length > 0)
                return Main.ClearFileName(PossibleTitles.First().Split('|')[0].Split('>').Last());

            return null;
        }

        public string[] GetChapterPages(string HTML) {
            const string MASK = "var images = [\"";
            int Index = HTML.IndexOf(MASK);
            string Cutted = HTML.Substring(Index + MASK.Length, HTML.Length - (Index + MASK.Length));

            //alt="Use o navegador Google Chrome
            const string MASK2 = "Use o navegador Google Chrome";
            List<string> Tags = new List<string>(Main.GetElementsByAttribute(HTML, "alt", MASK2, true));
            foreach (string TAG in Main.GetElementsByAttribute(Cutted, "alt", MASK2, true))
                Tags.Add(TAG);

            List<string> Pictures = new List<string>();
            foreach (string Element in Tags) {
                string Picture = Main.GetElementAttribute(Element, "src");
                if (!Pictures.Contains(Picture))
                    Pictures.Add(Picture);
            }

            const string MASK3 = ",\"url\":\"";
            if (Pictures.Count <= 3 && HTML.IndexOf(MASK3) >= 0) {
                Pictures = new List<string>();
                Index = 0;
                int MinIndex = 0;
                while (Index < HTML.Length) {
                    string Str = string.Empty;
                    Index = HTML.IndexOf(MASK3, Index);
                    Index += MASK3.Length;
                    if (Index < 0 || Index < MinIndex)
                        break;
                    MinIndex = Index;
                    while (true) {
                        char c = HTML[Index++];
                        if (c == '"')
                            break;
                        Str += c;
                    }
                    Str = Str.Replace("\\/", "/");
                    if (!Pictures.Contains(Str))
                        Pictures.Add(Str);
                }
            }
            if (Main.CanSort(Pictures))
                return Pictures.Distinct().OrderBy(x => int.Parse(Main.GetFileName(x))).ToArray();
            else
                return Pictures.ToArray();
        }


        public string[] GetChapters() {
            //data-html="true"
            string[] Elements = Main.GetElementsByAttribute(HTML, "data-html", "true");
            if (Elements.Length == 0) {
                Elements = Main.GetElementsByAttribute(HTML, "class", "capitulo");
            }
            List<string> Chapters = new List<string>();
            try {
                foreach (string Element in Elements) {
                    string RealTag = Main.GetElementAttribute(Element, "data-content");
                    string Tag = Main.GetElementsByClasses(RealTag, 0, "btn", "btn-success", "btn-white", "pull-left", "btn-small")[0];
                    string URL = Main.GetElementAttribute(Tag, "href");
                    Chapters.Add(URL);
                }
            } catch {

            }

            try {
                Elements = Main.GetElementsByClasses(HTML, 0, "capitulo");
                foreach (string Element in Elements) {
                    string CP = Main.GetElementAttribute(Element, "href");
                    if (string.IsNullOrWhiteSpace(CP) || Chapters.Contains(CP))
                        continue;
                    Chapters.Add(CP);
                }
            } catch {

            }
            return Chapters.ToArray();
        }

        public string GetChapterName(string URL) {
            int Index = URL.IndexOf("/manga/");
            URL = URL.Substring(Index, URL.Length - Index);
            return URL.Split('/')[3];
        }
    }
}
