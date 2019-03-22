using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;
using System.Windows.Forms;

namespace MangaUnhost.Host {
    class NHentai : IHost {
        string HTML;

        public bool NeedsProxy => AccCookies != null && AccCookies.Length < 2;

        public string HostName => "nhentai";

        public string DemoUrl => "http://nhentai.net/g/190997/";

        public CookieContainer Cookies => AccCookies != null ? AccCookies.ToContainer() : null;

        public string UserAgent => null;

        public string Referrer => null;

        public bool SelfChapterDownload => false;

        public string GetChapterName(string ChapterURL) {
            return "One Shot";
        }

        public string[] GetChapterPages(string HTML) {
            HTML = this.HTML;
            HTML = HTML.Substring(HTML.IndexOf("<div class=\"thumb-container\">"));
            string[] Elements = Main.GetElementsByClasses(HTML, "gallerythumb");

            List<string> Links = new List<string>();
            foreach (string Element in Elements) {
                bool SlowDown = false;
                bool Failed = false;
                Retry:;
                try {
                    if (SlowDown)
                        System.Threading.Thread.Sleep(1000);

                    string Page = (from x in Main.ExtractHtmlLinks(Element, "nhentai.net") where IsValidLink(x) select x).First();
                    string pHTML = Main.Download(Page.Replace("http:", "https:"), Encoding.UTF8, AllowRedirect: false, Cookies: Cookies);

                    Page = Main.GetElementsByClasses(pHTML, "fit-horizontal").First();
                    Links.Add(Main.ExtractHtmlLinks(Page, "nhentai.net").First());

                    Failed = false;
                } catch (Exception ex){
                    if (!Failed) {
                        SlowDown = true;
                        Failed = true;
                        SkipSlowDown();
                        goto Retry;
                    }
                    throw ex;
                }
            }

            return Links.ToArray();
        }

        public string[] GetChapters() {
            return new string[] { "http://nhentai.net" };
        }

        public string GetFullName() {
            string Element = Main.GetElementsByContent(HTML, "&raquo;").First();
            int Index = Element.IndexOf("&raquo;");

            string Name = HttpUtility.HtmlDecode(Element.Substring(0, Index).Split('>')[1].TrimEnd());

            return Main.GetRawNameFromUrlFolder(Name, true);
        }

        public string GetName(string CodedName) {
            return "Unk";
        }

        public string GetPosterUrl() {
            int Index = HTML.IndexOf("<div id=\"cover\">");

            string Pic = Main.GetElementsByAttribute(HTML, "data-src", "t.nhentai.net", ContainsOnly: true).First();

            Pic = Main.GetElementAttribute(Pic, "data-src");

            return Pic;
        }

        public void Initialize(string URL, out string Name, out string Page) {
            if (!IsValidLink(URL))
                throw new Exception();


            string ID = URL.Substring(URL.IndexOf("/g/") + 3).Split('/')[0];

           
            Page = $"https://nhentai.net/g/{ID}/";
            Name = "Unk";

            InputUrl = Page;
        }

        string InputUrl;

        public bool IsValidLink(string URL) {
            //https://nhentai.net/g/190997/
            URL = URL.ToLower();
            return Uri.IsWellFormedUriString(URL, UriKind.Absolute) && URL.Contains("nhentai") && URL.Contains("/g/");
        }

        //https://nhentai.net/login/
        Cookie[] AccCookies = null;
        public void LoadPage(string URL) {
            if (AccCookies == null) {
                if (Main.Instance.InvokeRequired) {
                    Main.Instance.Invoke(new MethodInvoker(() =>LoadPage(URL)));
                    return;
                }

                var Form = new Form() {
                    Size = new System.Drawing.Size(500, 600),
                    ShowIcon = false,
                    ShowInTaskbar = false,
                    Text = "Login into your account",
                    FormBorderStyle = FormBorderStyle.FixedToolWindow
                };
                var Browser = new WebBrowser() {
                    Dock = DockStyle.Fill,
                    ScriptErrorsSuppressed = true,
                    IsWebBrowserContextMenuEnabled = false,
                    AllowWebBrowserDrop = false,
                    Visible = false
                };
                var Message = new Label() {
                    Text = "Loading...",
                    Font = new System.Drawing.Font("Consola", 24),
                    Dock = DockStyle.Fill,
                    TextAlign = System.Drawing.ContentAlignment.MiddleCenter,
                    Visible = true
                };

                Form.Controls.Add(Browser);
                Form.Controls.Add(Message);

                Form.FormClosing += (a, b) => {
                    if (AccCookies == null)
                        b.Cancel = true;
                };

                Form.Show(Main.Instance);

                Browser.Navigate("https://nhentai.net/login/");
                Browser.WaitForLoad();
                while (Browser.Url.LocalPath.Trim('/').ToLower() == "login") {
                    Browser.WaitForLoad();

                    Message.Visible = false;
                    Browser.Visible = true;

                    Browser.WaitForRedirect();

                    Message.Visible = true;
                    Browser.Visible = false;

                    Browser.WaitForLoad();
                }
                
                var Cookies = Browser.GetCookies("nhentai.net");
                AccCookies = (from x in Cookies where x.Name == "csrftoken" || x.Name == "sessionid" select x).ToArray();

                Form.Close();
            }

            SkipSlowDown();

            HTML = Main.Download(URL, Encoding.UTF8, Cookies: Cookies);
        }

        private void SkipSlowDown() {
            WebBrowser Browser = null;
            Main.Instance.Invoke(new MethodInvoker(() => Browser = new WebBrowser()));
            Browser.AsyncNavigate(InputUrl);
            Browser.WaitForLoad();

            bool SlowDown = Browser.GetHtml().Contains("You're loading pages way too quickly");

            if (SlowDown) {
                Browser.Sleep();
                Browser.InjectAndRunScript("document.getElementsByClassName(\"button button-wide\")[0].click();");
                Browser.WaitForRedirect();
                Browser.WaitForLoad();
            }
        }

        Dictionary<string, bool> ProxyCache = new Dictionary<string, bool>();
        public bool ValidateProxy(string Proxy) {
            if (ProxyCache.ContainsKey(Proxy))
                return ProxyCache[Proxy];

            try {
                HttpWebRequest Request = WebRequest.Create(GetChapters()[0]) as HttpWebRequest;
                Request.Proxy = new WebProxy(Proxy);
                Request.Timeout = 10 * 1000;
                Request.Method = "GET";
                var Resp = Request.GetResponse() as HttpWebResponse;
                if (Resp.StatusCode != HttpStatusCode.OK || !Resp.ResponseUri.Host.ToLower().Contains("nhentai.net"))
                    return ProxyCache[Proxy] = false;
                return ProxyCache[Proxy] = true;
            } catch {
                return ProxyCache[Proxy] = false;
            }
        }
    }
}
