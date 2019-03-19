using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;
using System.Windows.Forms;
//using TempMailAPI;

namespace MangaUnhost.Host {
    class WebNovel : IHost {
        public string HostName => "WebNovel";

        public string DemoUrl => "https://www.webnovel.com/comic/11609847806441001";

        public bool NeedsProxy => false;

        public CookieContainer Cookies => null;

        public string UserAgent => null;

        public string Referrer => null;

        public bool SelfChapterDownload => true;

        public string GetChapterName(string ChapterURL) {
            return NameMap[ChapterURL];
        }

        public string[] GetChapterPages(string Link) {
            string HTML = string.Empty;
            if (LockMap[Link]) {
                //Unlock
                Login();
            } else {
                HTML = Main.Download(Link, Encoding.UTF8, UserAgent: Tools.UserAgent, Referrer: InputURL);
            }
            string[] Elms = Main.GetElementsByClasses("j_comic_img");

            List<string> Pages = new List<string>();
            foreach (string Elm in Elms) {
                string Page = Main.GetElementAttribute(Elm, "data-original");
                Pages.Add(Page);
            }

            return Pages.ToArray();
        }

        Dictionary<string, string> NameMap = new Dictionary<string, string>();
        Dictionary<string, bool> LockMap = new Dictionary<string, bool>();
        public string[] GetChapters() {
            string HTML = this.HTML;

            HTML = HTML.Substring(HTML.IndexOf("comicId"));
            HTML = HTML.Between('=', '"');

            string ComicId = HTML;

            HTML = this.HTML;
            HTML = HTML.Substring(HTML.IndexOf("firstChapterId"));
            HTML = HTML.Substring(0, HTML.IndexOf("\",\""));

            List<string> ChapterList = new List<string>();
            string ChapterID = HTML.Between('"', '"', 1);

            do {
                string URL = string.Format("https://www.webnovel.com/comic/{0}/{1}", ComicId, ChapterID);
                HTML = Main.Download(URL, Encoding.UTF8, UserAgent: Tools.UserAgent, Referrer: InputURL);

                ChapterID = HTML.Substring(HTML.IndexOf("nextId")).Between('\'', '\'');
                ChapterList.Add(URL);

                bool Locked = HTML.Contains("data-islock=\"1\"");
                string Name = HTML.Substring(HTML.IndexOf("cha-hd-progress"));
                Name = Name.Substring(0, Name.IndexOf("</div>"));
                Name = Name.Substring(Name.LastIndexOf("<span>"));
                Name = Name.Between('>', '<');

                NameMap[URL] = Name;
                LockMap[URL] = Locked;

            } while (ChapterID != "0");
            
            return ChapterList.ToArray().Reverse().ToArray();
        }

        public string GetFullName() {
            string HTML = this.HTML;
            HTML = HTML.Substring(HTML.IndexOf("auto_height"));
            return HTML.Between('>', '<');
        }

        public string GetName(string CodedName) {
            return Main.GetRawNameFromUrlFolder(CodedName);
        }

        public string GetPosterUrl() {
            string HTML = this.HTML;
            HTML = HTML.Substring(HTML.IndexOf("g_thumb"));
            HTML = HTML.Substring(0, HTML.IndexOf("</i>"));

            string Link = Main.ExtractHtmlLinks(HTML, "img.webnovel.com", "srcset").Last();
            return Link;
        }

        public void Initialize(string URL, out string Name, out string Page) {
            if (!IsValidLink(URL))
                throw new Exception();

            Name = "Manga";
            Page = URL;
            InputURL = URL;
        }

        public bool IsValidLink(string URL) {
            return URL.ToLower().StartsWith("http") && URL.ToLower().Contains("webnovel.com/comic/") && !URL.ToLower().EndsWith("/comic/");
        }

        public void LoadPage(string URL) {
            Main.Instance.Invoke(new MethodInvoker(() => {
                Browser = new WebBrowser();
                Browser.ScriptErrorsSuppressed = true;
                Browser.Navigate(URL);
                Browser.WaitForLoad();
            }));
        }

        string InputURL;

        string HTML { get {
                string Content = null;
                Browser.Invoke(new MethodInvoker(() => {
                    Browser.WaitForLoad();
                    Content = Browser.Document.GetElementsByTagName("HTML")[0].OuterHtml;
                }));
                return Content;
            }
        }
        WebBrowser Browser;

        public bool ValidateProxy(string Proxy) {
            throw new NotImplementedException();
        }

        //https://passport.webnovel.com/login.html
        void Login() {
            Browser.Invoke(new MethodInvoker(() => {
                Browser.Navigate(InputURL);
                Browser.WaitForLoad();
            }));

            string HTML = this.HTML;
            HTML = HTML.Substring(0, HTML.IndexOf("<div class=\"oh\">"));

            if (HTML.Contains("Log out")) {
                Browser.InjectAndRunScript("var Logout = document.getElementsByClassName(\"j_logout\")[0];Logout.click();");
            }

            NewAccount();

        }

        Account CurrentAccount;
        //TempMail Email;
        private void NewAccount() {
        //    Email = new TempMail();
            CurrentAccount = new Account();
        //    CurrentAccount.Email = Email.User + "@" + Email.Domain;
            CurrentAccount.Password = CreatePassword();

            bool Failed = false;

            Browser.Invoke(new MethodInvoker(() => {
                Browser.Navigate("https://passport.webnovel.com/login.html");
                Browser.WaitForLoad();

                //Go to Signup page
                string HTML = this.HTML;
                HTML = HTML.Substring(HTML.IndexOf("extra-txt"));
                HTML = HTML.Substring(HTML.IndexOf("href=\""));
                string Register = HTML.Between('"', '"');
                Register = HttpUtility.HtmlDecode(Register);

                Browser.Navigate(Register);
                Browser.WaitForLoad();

                //Go to Email Signup Page
                HTML = this.HTML;
                HTML = HTML.Substring(HTML.IndexOf("bt bt-block _e"));
                HTML = HTML.Substring(HTML.IndexOf("href=\""));
                Register = HTML.Between('"', '"');
                Register = HttpUtility.HtmlDecode(Register);

                Browser.Navigate(Register);
                Browser.WaitForLoad();

                Browser.Sleep();

                Browser.InjectAndRunScript($"document.getElementsByName(\"email\")[0].value =    \"{CurrentAccount.Email}\";");
                Browser.InjectAndRunScript($"document.getElementsByName(\"password\")[0].value = \"{CurrentAccount.Password}\";");
                Browser.InjectAndRunScript("document.forms[0].submit();");

                int Loops = 0;
                while (!Browser.Url.AbsoluteUri.Contains("checkemail.html") && ++Loops <= 5)
                    Browser.Sleep();

                if (Loops > 5) {
                    Failed = true;
                    return;
                }

             //   var Mails = Email.GetEmailsReceived().ToArray();
            }));

            if (Failed)
                NewAccount();
        }
        string CreatePassword(int length = 10) {
            const string valid = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890";
            StringBuilder res = new StringBuilder();
            Random rnd = new Random();
            while (0 < length--) {
                res.Append(valid[rnd.Next(valid.Length)]);
            }
            return res.ToString();
        }

        struct Account {
            public string Email;
            public string Password;
        }
    }
}
