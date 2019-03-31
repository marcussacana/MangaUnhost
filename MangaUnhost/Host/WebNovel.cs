using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Web;
using System.Windows.Forms;

namespace MangaUnhost.Host {
    /// <summary>
    /// This plugin is slow because he is a bot to create an fake account and earn points
    /// </summary>
    class WebNovel : IHost {
        static bool Initialized = false;
        public WebNovel() {
            if (Initialized)
                return;

            Initialized = true;

            new Thread(() => {
                var Secondary = new WebNovel();
                Secondary.LoadPage("https://www.webnovel.com");
                Secondary.ClaimFakes();
            }).Start();
        }

        public string HostName => "WebNovel";

        public string DemoUrl => "https://www.webnovel.com/comic/11609847806441001";

        public bool NeedsProxy => false;

        public CookieContainer Cookies => CurrentCookies == null ? null : CurrentCookies.ToContainer();

        public string UserAgent => UA;

        public string Referrer => Ref ?? InputURL;

        public bool SelfChapterDownload => true;

        string FakeList = AppDomain.CurrentDomain.BaseDirectory + "WebNovelAccounts.ini";


        public string GetChapterName(string ChapterURL) {
            return NameMap[ChapterURL];
        }

        public string[] GenericComments = new string[] {
            "I'm loving this work.",
            "I loved this introduction.",
            "It looks promising.",
            "Hmm, I think I have to read some more to know if I'll like it...",
            "I did not like the protagonist so much.",
            "I liked the protagonist.",
            "I'm expecting.",
            "So far without expectations.",
            "It started very well.",
            "The Author started on the right foot."
        };

        public string[] GenericReplys = new string[] {
            "I agree.",
            "Indeed.",
            "Truth.",
            "I also think.",
            "I disagree.",
            "Do not.",
            "I think not.",
            "Maybe...",
            "Who knows."
        };


        public string[] GetChapterPages(string Link) {
            Ref = null;

            string HTML = string.Empty;
            if (LockMap[Link]) {
                UpdateStones();

                if (GetChapterPrice(Link) > AvaliableStones) {
                    Login();
                    EarnPoints();
                    UpdateStones();
                }

                if (GetChapterPrice(Link) > AvaliableStones)
                {
                    Main.Instance.Status = "Missing Stones...";
                }

                Unlock(Link);
            }

            ComicReader Reader;
            while (true) {
                try {
                    HTML = Main.Download(Link, Encoding.UTF8, Cookies: Cookies, UserAgent: UA, Referrer: InputURL);
                    const string Prefix = "var chapInfo =";
                    string Script = null;
                    Script = Main.GetElementsByContent(HTML, "\"chapterPage\":", SkipJavascript: false).First();

                    Script = Script.Substring(Script.IndexOf(Prefix) + Prefix.Length);
                    Script = Script.Substring(0, Script.IndexOf("};") + 1);

                    Script = Script.Replace("\\/", "/").Replace("\\&", "&").Replace("\\ ", " ").Replace("\\\\", "\\");
                    Reader = Extensions.JsonDecode<ComicReader>(Script);

                    break;
                } catch {

                }
            }

            string[] Pages = (from x in Reader.chapterInfo.chapterPage select x.url).ToArray();

            if (Reader.user.SS.HasValue)
                AvaliableStones = Reader.user.SS.Value;

            if (Pages.Length > 0)
                LockMap[Link] = false;

            Ref = Link;


            return Pages;
        }

        private void EarnPoints(WebBrowser Browser = null, bool NewAccount = true) {
            if (Browser == null)
                Browser = this.Browser;

            string Status = Main.Instance.Status;

            Main.Instance.Status = "Earning Stones...";

            //Add to library and give power vote
            Browser.AsyncNavigate("https://www.webnovel.com/book/12333953306291305");//Only book allow comment, review and vote with stone.
            Browser.WaitForLoad();

            if (NewAccount)
            {
                Browser.InjectAndRunScript("document.getElementsByClassName(\"_addLib\")[0].getElementsByTagName(\"span\")[0].click();");
                Browser.Sleep();
            }

            Browser.InjectAndRunScript("document.getElementsByClassName(\"j_vote_power\")[0].click();");
            Browser.Sleep();

            const string ExampleBook = "https://www.webnovel.com/book/12333953306291305/35097438813479503";

            if (NewAccount)
            {
                //Use 5 SS for the first time to recive 20
                Unlock(ExampleBook, true, Browser);

                //Send 5 Stars vote
                Browser.InjectAndRunScript("document.getElementsByClassName(\"j_rate\")[0].click();");
                Browser.Sleep();
                Browser.InjectAndRunScript("var Stars = document.getElementsByClassName(\"mb16 pt24 fs32\")[0];Stars.getElementsByTagName(\"input\")[0].removeAttribute(\"checked\");Stars.getElementsByTagName(\"input\")[5].checked = true; Stars.getElementsByTagName(\"input\")[5].setAttribute(\"checked\", true);Stars.getElementsByTagName(\"input\")[5].click();document.getElementsByClassName(\"bt bt_block j_chap_rate\")[0].click();");
                Browser.Sleep();



                /*//Give Only XP
                //Show Comments
                Browser.InjectAndRunScript("document.getElementsByClassName(\"j_bottom_comments\")[0].click();");
                Browser.Sleep();

                //Comment something
                const string PostCommentScript = "var Comment = document.getElementsByClassName(\"_scroller\")[0].getElementsByTagName(\"textarea\")[0];Comment.focus();Comment.value = \"{0}\";var Submit = Comment.form.elements[Comment.form.elements.length - 1];Submit.disabled = false;Submit.click();";
                Browser.InjectAndRunScript(string.Format(PostCommentScript, GenericComments.GetRandomElement()));
                Browser.Sleep();

                //Reply a random comment
                Browser.InjectAndRunScript($"document.getElementsByClassName(\"j_reply\")[0].click();");
                Browser.InjectAndRunScript(string.Format(PostCommentScript, GenericReplys.GetRandomElement()));
                Browser.Sleep();

                //*/

            }

            //Use Energy
            Browser.AsyncNavigate("https://www.webnovel.com/vote");
            Browser.WaitForLoad();
            Browser.InjectAndRunScript("document.getElementsByClassName(\"_voteBtn\")[0].click();");
            Browser.Sleep();

            //Show Claim Window (Daily tab)
            Browser.InjectAndRunScript("document.getElementsByClassName(\"_check_in dib j_show_task_mod\")[0].click();");
            Browser.Sleep();
            Browser.InjectAndRunScript("var Claim = document.getElementsByClassName(\"j_claim_task\"); for (var i = 0; i < Claim.length; i++) Claim[i].click();");
            Browser.Sleep(5);

            if (NewAccount)
            {

                //Claim Upgrade Tab
                Browser.InjectAndRunScript("document.getElementsByClassName(\"j_task_nav g_tab_nav dib _slide\")[0].getElementsByTagName(\"a\")[1].click();");
                Browser.Sleep();
                Browser.InjectAndRunScript("var Claim = document.getElementsByClassName(\"j_claim_task\"); for (var i = 0; i < Claim.length; i++) Claim[i].click();");
                Browser.Sleep(5);

                UpdateStones(Browser);
            }


            Main.Instance.Status = Status;
        }

        private void UpdateStones(WebBrowser Browser = null) {
            if (Browser == null)
                Browser = this.Browser;

            if (CurrentCookies == null) {
                AvaliableStones = 0;
                return;
            }

            string HTML = Main.Download(InputURL, Encoding.UTF8, UserAgent: UA, Referrer: InputURL, Cookies: Cookies);
            HTML = HTML.Substring(HTML.IndexOf("data-ss="));

            string SS = HTML.Between('"', '"');

            if (int.TryParse(SS, out int Stones)) {
                AvaliableStones = Stones;
                return;
            }
            

            AvaliableStones = 0;
        }

        private int GetChapterPrice(string Chapter) {
            string HTML = Main.Download(Chapter, Encoding.UTF8, UserAgent: UA, Referrer: InputURL, Cookies: Cookies);

            int Index = HTML.IndexOf("\"price\"");
            if (Index < 0)
                Index = HTML.IndexOf("\"SSPrice\"");
            if (Index < 0)
                return 0;

            HTML = HTML.Substring(Index);

            string SS = HTML.Between(':', ',');

            if (int.TryParse(SS, out int Stones)) {
                return Stones;
            }

            return 0;
        }

        private void Unlock(string Chapter, bool Hide = false, WebBrowser Browser = null) {
            if (Browser == null)
                Browser = this.Browser;

            string Status = Main.Instance.Status;
            if (!Hide)
                Main.Instance.Status = "Unlocking Chapter...";

            UpdateStones();

            if (AvaliableStones < GetChapterPrice(Chapter))
                return;

            int Stones = AvaliableStones;

            while (Stones == AvaliableStones)
            {

                Browser.Navigate(Chapter);
                Browser.WaitForLoad();

                if (Chapter.ToLower().Contains("/book/"))
                {
                    Browser.InjectAndRunScript("document.getElementsByClassName(\"_bt_unlock\")[0].click();");
                }
                else
                {
                    Browser.InjectAndRunScript("document.getElementsByClassName(\"j_unlock\")[0].click();");
                }

                Browser.Sleep(5);

                UpdateStones();
            }

            Main.Instance.Status = Status;
        }

        string UA;
        string InputURL;
        string Ref;
        Cookie[] CurrentCookies;
        WebBrowser Browser;
        Account CurrentAccount;
        GuerrillaMail Email;
        string FirstChapterUrl;
        int AvaliableStones = 30;

        Dictionary<string, string> NameMap = new Dictionary<string, string>();
        Dictionary<string, bool> LockMap = new Dictionary<string, bool>();

        public string[] GetChapters() {
            string HTML = GetHTML();

            HTML = HTML.Substring(HTML.IndexOf("comicId"));
            HTML = HTML.Between('=', '"');

            string ComicId = HTML;

            HTML = GetHTML();
            HTML = HTML.Substring(HTML.IndexOf("firstChapterId"));
            HTML = HTML.Substring(0, HTML.IndexOf("\",\""));

            List<string> ChapterList = new List<string>();
            string ChapterID = HTML.Between('"', '"', 1);

            do {
                string URL = string.Format("https://www.webnovel.com/comic/{0}/{1}", ComicId, ChapterID);
                if (string.IsNullOrEmpty(FirstChapterUrl))
                    FirstChapterUrl = URL;

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
            string HTML = GetHTML();
            HTML = HTML.Substring(HTML.IndexOf("auto_height"));
            return HTML.Between('>', '<');
        }

        public string GetName(string CodedName) {
            return Main.GetRawNameFromUrlFolder(CodedName);
        }

        public string GetPosterUrl() {
            string HTML = GetHTML();
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
            while (Main.Instance == null)
                Thread.Sleep(100);

            Main.Instance.Invoke(new MethodInvoker(() => {
                Browser = new WebBrowser();
                Browser.ScriptErrorsSuppressed = true;
                Browser.Navigate(URL);
                Browser.WaitForLoad();
                UA = Browser.GetUserAgent();
            }));
        }

        private string GetHTML(WebBrowser Browser = null)
        {
            if (Browser == null)
                Browser = this.Browser;

            string Content = null;
            Browser.Invoke(new MethodInvoker(() => {
                Browser.WaitForLoad();
                Content = Browser.Document.GetElementsByTagName("HTML")[0].OuterHtml;
            }));
            return Content;
        }


        public bool ValidateProxy(string Proxy) {
            throw new NotImplementedException();
        }

        //https://passport.webnovel.com/login.html
        void Login(WebBrowser Browser = null, Account? Account = null) {
            if (Browser == null)
                Browser = this.Browser;

            string Status = Main.Instance.Status;
            Browser.AsyncNavigate("https://www.webnovel.com/");
            Browser.WaitForLoad();

            string HTML = GetHTML(Browser);
            //HTML = HTML.Substring(0, HTML.IndexOf("<div class=\"oh\">"));

            if (HTML.Contains("g_drop_link\"><a href=\"/profile")) {
                Browser.InjectAndRunScript("var Logout = document.getElementsByClassName(\"j_logout\")[0];Logout.click();");
                Browser.WaitForRedirect();
                Browser.WaitForLoad();
            }

            if (!Account.HasValue)
            {
                NewAccount(Browser);

                Main.Instance.Status = "Finishing Account Registration...";
            }

            bool InRetry = false;

            Retry:;

            if (InRetry && Email != null)
                ConfirmEmail(Email.GetAllEmails(), Browser);

            Browser.AsyncNavigate("https://passport.webnovel.com/login.html");
            Browser.WaitForLoad();
            Browser.Sleep(5);

            if (Browser.Url.AbsoluteUri.Contains("login.html")) {
                SignIn(Account);

                InRetry = true;

                goto Retry;
            }


            if (!Account.HasValue)
            {
                CurrentCookies = Browser.GetCookies();


                Browser.Sleep(5);
                Browser.InjectAndRunScript($"document.getElementsByClassName(\"j_name\")[0].value = \"{CreatePassword()}\";var post = document.getElementsByClassName(\"bt bt_block\");post = post[post.length - 1];post.click();");
                Browser.Sleep(5);
            }

            Main.Instance.Status = Status;

        }

        private void SignIn(Account? Account, WebBrowser Browser = null)
        {
            if (Browser == null)
                Browser = this.Browser;

            if (!Account.HasValue)
                Account = CurrentAccount;

            if (!Browser.Url.AbsoluteUri.Contains("login.html"))
            {
                Browser.AsyncNavigate("https://passport.webnovel.com/login.html");
                Browser.WaitForLoad();
                Browser.Sleep(5);
            }


            string HTML = GetHTML(Browser);

            HTML = HTML.Substring(HTML.IndexOf("with Twitter") + 1);
            HTML = HTML.Substring(HTML.IndexOf("with Twitter"));

            string WithEmail = Main.ExtractHtmlLinks(HTML, "passport.webnovel.com", "href").First();

            Browser.AsyncNavigate(WithEmail);
            Browser.WaitForLoad();

            Browser.InjectAndRunScript($"document.getElementsByName(\"email\")[0].value =    \"{Account?.Email}\";");
            Browser.InjectAndRunScript($"document.getElementsByName(\"password\")[0].value = \"{Account?.Password}\";");
            Browser.Sleep();
            Browser.InjectAndRunScript("LoginV1.checkCode();");
            Browser.InjectAndRunScript("document.forms[0].submit();");
            Browser.Sleep(5);
            Browser.WaitForLoad();
        }

        private void NewAccount(WebBrowser Browser = null) {
            if (Browser == null)
                Browser = this.Browser;

            string Status = Main.Instance.Status;
            Main.Instance.Status = "Creating new account...";

            Email = new GuerrillaMail();
            CurrentAccount = new Account();
            CurrentAccount.Email = Email.GetMyEmail();
            CurrentAccount.Password = CreatePassword();

            bool Failed = false;

            try {

                Browser.AsyncNavigate("https://passport.webnovel.com/login.html");
                Browser.WaitForLoad();

                //Go to Signup page
                string HTML = GetHTML(Browser);
                HTML = HTML.Substring(HTML.IndexOf("extra-txt"));
                HTML = HTML.Substring(HTML.IndexOf("href=\""));
                string Register = HTML.Between('"', '"');
                Register = HttpUtility.HtmlDecode(Register);

                Browser.AsyncNavigate(Register);
                Browser.WaitForLoad();

                //Go to Email Signup Page
                HTML = GetHTML(Browser);
                HTML = HTML.Substring(HTML.IndexOf("bt bt-block _e"));
                HTML = HTML.Substring(HTML.IndexOf("href=\""));
                Register = HTML.Between('"', '"');
                Register = HttpUtility.HtmlDecode(Register);

                Browser.AsyncNavigate(Register);
                Browser.WaitForLoad();

                Browser.Sleep();

                Browser.InjectAndRunScript($"document.getElementsByName(\"email\")[0].value =    \"{CurrentAccount.Email}\";");
                Browser.InjectAndRunScript($"document.getElementsByName(\"password\")[0].value = \"{CurrentAccount.Password}\";");




                int Retries = 0;
                again:;
                Browser.Sleep();
                Browser.InjectAndRunScript("LoginV1.register();");
                Browser.InjectAndRunScript("document.forms[0].submit();");

                int Loops = 0;
                while (!Browser.Url.AbsoluteUri.Contains("checkemail.html") && ++Loops <= 5)
                    Browser.Sleep();

                if (Loops > 5) {
                    if (Retries++ < 3)
                        goto again;
                    Failed = true;
                    return;
                }



                Main.Instance.Status = "Waiting Account Verification Email...";

                Loops = 0;
                var Mails = new List<GuerrillaMail.Email>();
                while (Mails.Count == 0) {
                    Browser.Sleep(10);
                    if (Loops++ > 60) {//10 * 60 = 600 (10min)
                        throw new Exception();
                    }
                    Mails = Email.GetAllEmails();
                }

                ConfirmEmail(Mails);

            } catch {
                Failed = true;
            }

            if (Failed)
                NewAccount(Browser);

            SaveFake();

            Main.Instance.Status = Status;
        }

        private void ConfirmEmail(List<GuerrillaMail.Email> Mails, WebBrowser Browser = null) {
            if (Browser == null)
                Browser = this.Browser;

            string Status = Main.Instance.Status;
            Main.Instance.Status = "Confirming Email...";

            if (Mails.Count != 0) {
                var Activation = Email.GetEmail(Mails.Single().mail_id);
                string Content = Activation.mail_body.Substring(Activation.mail_body.IndexOf("<a href=\"http"));
                string ActivationLink = Main.ExtractHtmlLinks(Content, null, "href").First();

                Browser.AsyncNavigate(ActivationLink);
                Browser.WaitForLoad();
                Browser.WaitForRedirect();

                Browser.WaitForLoad();
                Browser.Sleep();
            }

            Main.Instance.Status = Status;
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

        void SaveFake()
        {
            string Entries = Ini.GetConfig("WebNovel", "Entries", FakeList, false);

            int ID = 0;

            if (Entries == string.Empty)
            {
                Ini.SetConfig("WebNovel", "Entries", "1", FakeList);
            }
            else
            {
                ID = int.Parse(Entries);
                Ini.SetConfig("WebNovel", "Entries", (ID + 1).ToString(), FakeList);
            }

            Ini.SetConfig($"Fake.{ID}", "Password", CurrentAccount.Password, FakeList);
            Ini.SetConfig($"Fake.{ID}", "Email", CurrentAccount.Email, FakeList);

        }


        private void ClaimFakes()
        {
            while (Main.Instance == null)
                Thread.Sleep(100);

            string Entries = Ini.GetConfig("WebNovel", "Entries", FakeList, false);
            if (Entries == string.Empty)
                return;


            int Count = int.Parse(Entries);

            if (Count == 0)
                return;

            bool Claim = false;
            Main.Instance.Invoke(new MethodInvoker(() => {
                Claim = MessageBox.Show("Claim Dialy stones of all fake accounts?", "Mangaunhost", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes;
            }));

            if (!Claim)
                return;

            string Status = Main.Instance.Status;

            for (int i = 0; i < Count; i++)
            {
                Main.Instance.Status = $"Reciving Dialy Stones ({i}/{Count})...";

                string Email = Ini.GetConfig($"Fake.{i}", "Email", FakeList);
                string Pass = Ini.GetConfig($"Fake.{i}", "Password", FakeList);

                WebBrowser Browser = null;
                Main.Instance.Invoke(new MethodInvoker(() => { Browser = new WebBrowser(); Browser.ScriptErrorsSuppressed = true; }));


                Login(Browser, new Account() { Email = Email, Password = Pass});
                EarnPoints(Browser, false);
                
            }

            Main.Instance.Status = Status;
        }

        struct Account {
            public string Email;
            public string Password;
        }

#pragma warning disable 649
        struct ComicReader {
            public ComicInfo comicInfo;
            public ChapterInfo chapterInfo;
            public Settings settings;
            public User user;
        }
        struct ComicInfo {
            public string comicId;
            public string comicName;
            public int chapterNum;
            public int actionStatus;
            public string publisher;
            public int auditStatus;
            public int novelType;
            public string CV;
            public int inLibrary;
        }
        struct ChapterInfo {
            public string chapterId;
            public string chapterName;
            public int chapterIndex;
            public int price;
            public int pageCount;
            public string preChapterId;
            public string nextChapterId;
            public int isVip;
            public int isAuth;
            public ChapterPage[] chapterPage;
        }
        struct ChapterPage {
            public string pageId;
            public int height;
            public int width;
            public string url;
        }
        struct Settings {
            public string tc;
            public string tf;
            public string ts;
        }
        struct User {
            public string avatar;
            public string nickName;
            public string userName;
            public string guid;
            public int? status;
            public int? role;
            public string penName;
            public int? SS;
            public int? vSS;
            public int? vbSS;
            public int? bSS;
            public int? grade;
            public int? ES;
            public int? totalES;
            public int? PS;
            public int? totalPS;
            public int? emailStatus;
            public int? isCheckIn;
            public long? UUT;
        }
#pragma warning restore 649
    }
}
