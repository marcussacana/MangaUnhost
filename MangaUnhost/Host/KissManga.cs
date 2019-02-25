using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;

namespace MangaUnhost.Host {
    class KissManga : IHost {
        public string HostName => "KissManga";

        public string DemoUrl => "https://kissmanga.com/Manga/Orenchi-ni-kita-Onna-kishi-to-Inakagurashi-suru-koto-ni-natta-ken";

        public bool NeedsProxy => false;
        public CookieContainer Cookies => new Cookie(CookieName, Cookie, "/", Host).ToContainer();

        public string UserAgent => UA;

        public string Referrer => null;

        public string GetChapterName(string ChapterURL) {
            const string Prefix = "/manga/";
            string Part = ChapterURL.Substring(ChapterURL.ToLower().IndexOf(Prefix) + Prefix.Length);
            Part = Part.Split('/')[1];
            string Rst = string.Empty;
            foreach (char c in Part) {
                if (Rst == string.Empty && !char.IsNumber(c))
                    continue;
                if (c == '-') {
                    Rst += '.';
                    continue;
                }
                if (!char.IsNumber(c))
                    break;
                Rst += c;
            }
            return string.Join(".", (from x in Rst.Trim('.').Split('.') select x.TrimStart('0')).ToArray());
        }

        private string _key = null;
        private string Key {
            get {
                if (_key != null)
                    return _key;

                string Key = "mshsdf832nsdbash20asdm";
                string[] Tags = Main.GetElementsByContent(PHTML, "chko", SkipJavascript: false);
                string chko = string.Empty;
                for (int i = 0; i < Tags.Length; i++) {
                    Tags[i] = Tags[i].Between('>', '<').Trim();
                    Tags[i] = Tags[i].Beautifier();

                    if (Tags[i].Contains("chko = chko"))
                        chko = chko + Tags[i].Split('\'')[1];
                    else 
                        chko = Tags[i].Split('\'')[1];
                    
                }
                _key = chko == string.Empty ? Key : chko;
                return _key;
            }
        }

        private string PHTML = null;
        private string IV = "a5 e8 e2 e9 c2 72 1b e0 a8 4a d6 60 c4 72 c1 f3";
        public string[] GetChapterPages(string HTML) {
            PHTML = HTML;
            _key = null;

            List<string> Pages = new List<string>();
            const string Prefix = "wrapKA(\"";
            while (HTML.IndexOf(Prefix) >= 0) {
                HTML = HTML.Substring(HTML.IndexOf(Prefix) + Prefix.Length);
                string Code = HTML.Split('"')[0];
                Pages.Add(DecryptAesB64(Code));
            }

            return Pages.ToArray();
        }

        private string DecryptAesB64(string Content) {
            var SHA = new SHA256Managed();
            byte[] Key = SHA.ComputeHash(Encoding.ASCII.GetBytes(this.Key));
            var AES = new AesManaged {
                Key = Key,
                IV = (from x in IV.Split(' ') select Convert.ToByte(x, 16)).ToArray(),
                Mode = CipherMode.CBC,
                Padding = PaddingMode.PKCS7
            };

            var data = Convert.FromBase64String(Content);
            using (MemoryStream Input = new MemoryStream(data))
            using (MemoryStream Stream = new MemoryStream())
            using (CryptoStream Decrypor = new CryptoStream(Stream, AES.CreateDecryptor(), CryptoStreamMode.Write)) {
                Input.CopyTo(Decrypor);
                Decrypor.FlushFinalBlock();
                return Encoding.UTF8.GetString(Stream.ToArray()).Replace("\x10", "").Replace("\x0f", "");
            }
        }

        public string[] GetChapters() {
            string HTML = this.HTML.Substring(this.HTML.IndexOf("<tr style="));
            HTML = HTML.Substring(0, HTML.LastIndexOf("<td>"));
            string[] Links = Main.ExtractHtmlLinks(HTML, "kissmanga.com");

            return Links;
        }

        public string GetFullName() {
            return HTML.Substring(HTML.IndexOf("<a Class=\"bigChar\" href=\"")).Between('>', '<');
        }

        public string GetName(string CodedName) {
            int tmp;
            if (int.TryParse(CodedName, out tmp))
                CodedName = "Unk";

            return Main.GetRawNameFromUrlFolder(CodedName);
        }

        public string GetPosterUrl() {
            string HTML = this.HTML.Substring(this.HTML.IndexOf("<img width="));
            string Url = Main.ExtractHtmlLinks(HTML, "kissmanga.com")[0];
            return Url;
        }

        public void Initialize(string URL, out string Name, out string Page) {
            if (!IsValidLink(URL))
                throw new Exception();

            const string Prefix = "/manga/";
            int Index = URL.ToLower().IndexOf(Prefix);
            if (Index < 0)
                throw new Exception();
            Index += Prefix.Length;

            string[] Parts = URL.Substring(Index).Split('/');
            Name = GetName(Parts[0]);
            if (Parts.Length > 1 && Parts[1].Length > 0) {
                URL = URL.Substring(0, URL.IndexOf(Parts[1]));
            }
            Page = URL;
        }

        public bool IsValidLink(string URL) {
            URL = URL.ToLower();
            return Uri.IsWellFormedUriString(URL, UriKind.Absolute) && URL.Contains("kissmanga.com") && URL.Contains("/manga/");
        }

        const string CookieName = "cf_clearance";
        string HTML = null;
        string Cookie = null;
        string Host = null;
        string UA = null;
        public void LoadPage(string URL) {
            if (Cookie == null)
                Main.Instance.Invoke(new MethodInvoker(() => { Cookie = AuthBrowser(URL); }));

            Host = new Uri(URL).Host;

            HTML = Main.Download(URL, Encoding.UTF8, UserAgent: UserAgent, Cookies: Cookies);
        }

        string AuthBrowser(string URL) {
            var Browser = new WebBrowser() {
                ScriptErrorsSuppressed = true
            };

            Browser.Navigate(URL);
            Browser.WaitForRedirect();
            Browser.WaitForLoad();
            bool Fail = Browser.DocumentText.Contains("Please wait 5 seconds...");
            if (Fail)
                throw new Exception("Failed to Bypass the Anti-Bot");

            UA = (string)Browser.InjectAndRunScript("return clientInformation.userAgent;");

            return Browser.GetCookies().Get(CookieName);
        }

        public bool ValidateProxy(string Proxy) {
            throw new NotImplementedException();
        }
    }
}
