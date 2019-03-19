using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;
using System.Windows.Forms;

namespace MangaUnhost.Host {

    class Tsumino : IHost {
        public bool NeedsProxy => false;

        public string HostName => "Tsumino";

        public string DemoUrl => "https://www.tsumino.com/Book/Info/7691/genko-no-ori";

        public CookieContainer Cookies => null;

        public string UserAgent => null;

        public string Referrer => null;

        public bool SelfChapterDownload => false;

        string HTML;
        static string Token;
        string MainPage;

        const string CookieName = "ASP.NET_SessionId";
        public string GetChapterName(string ChapterURL) {
            if (NameMap.ContainsKey(ChapterURL))
                return NameMap[ChapterURL];

            return "One Shot";
        }

        public string[] GetChapterPages(string HTML) {
            const string Prefix = "http://www.tsumino.com/Image/Thumb/";
            int Index = HTML.IndexOf(Prefix) + Prefix.Length;
            int EndInd = HTML.IndexOf("\"", Index);
            string ID = HTML.Substring(Index, EndInd - Index);


            string Info = null;

            for (int i = 0; i < 6 && Info == null; i++) {
                Info = RequestInfo(ID);
                if (!string.IsNullOrEmpty(Info))
                    break;
            }

            const string ListPrefix = "\"reader_page_urls\":[";
            Index = Info.IndexOf(ListPrefix);
            if (Index < 0)
                throw new Exception();
            Index += ListPrefix.Length;

            string List = Info.Substring(Index).Split(']')[0].Replace("\",\"", "\x0").Trim('"');

            string[] Names = List.Split('\x0');

            List<string> Links = new List<string>();
            foreach (string Name in Names) {
                Links.Add($"https://www.tsumino.com/Image/Object?name={HttpUtility.UrlEncode(Name)}");
            }

            return Links.ToArray();
            
        }


        private string RequestInfo(string ID) {
            try {
                HttpWebRequest Request = (HttpWebRequest)WebRequest.Create("http://www.tsumino.com/Read/Load");
                Request.Method = "POST";
                Request.UserAgent = Tools.UserAgent;
                Request.CookieContainer = new CookieContainer();
                Request.CookieContainer.Add(Request.RequestUri, new Cookie(CookieName, GetToken(ID)));
                Request.ContentType = "application/x-www-form-urlencoded; charset=UTF-8";
                byte[] Buffer = Encoding.UTF8.GetBytes($"q={ID}");
                var Stream = Request.GetRequestStream();
                Stream.Write(Buffer, 0, Buffer.Length);
                Stream.Close();
                Stream = Request.GetResponse().GetResponseStream();

                var Tmp = new MemoryStream();
                Stream.CopyTo(Tmp);

                Buffer = Tmp.ToArray();

                return Encoding.UTF8.GetString(Buffer);
            } catch {
                Token = null;
            }

            return null;
        }

        private string GetToken(string ID) {
            bool TokenActivated = true;
            if (Token == null) {
                TokenActivated = ReqToken(ID);
            }

            if (Token == null || !TokenActivated) {
                Form Main = Application.OpenForms[0];
                Main.Invoke(new MethodInvoker(() => {
                    var Form = new Form() {
                        Size = new System.Drawing.Size(500, 600),
                        ShowIcon = false,
                        ShowInTaskbar = false,
                        Text = "Solve the Captcha",
                        FormBorderStyle = FormBorderStyle.FixedToolWindow
                    };
                    var Browser = new WebBrowser() {
                        Dock = DockStyle.Fill,
                        ScriptErrorsSuppressed = true,
                        IsWebBrowserContextMenuEnabled = false,
                        AllowWebBrowserDrop = false
                    };
                    var Message = new Label() {
                        Text = "Processing...",
                        Font = new System.Drawing.Font("Consola", 24),
                        Dock = DockStyle.Fill,
                        TextAlign = System.Drawing.ContentAlignment.MiddleCenter
                    };

                    Form.Controls.Add(Browser);

                    Browser.Navigate($"https://www.tsumino.com/Read/Auth/{ID}");
                    Browser.WaitForLoad();
                    Browser.InjectAndRunScript("var Button = document.getElementsByClassName('book-read-button')[0];Button.setAttribute('value', 'Solve the Captcha...');Button.disabled = true;");
                    Form.Show(Main);

                    int Check = 0;
                    string CaptchaResult = null;
                    while (string.IsNullOrEmpty(CaptchaResult)) {
                        Application.DoEvents();
                        System.Threading.Thread.Sleep(2);
                        Check++;
                        if (Check < 100)
                            continue;
                        Check = 0;
                        object rst = Browser.InjectAndRunScript("return grecaptcha.getResponse();");
                        if (rst == null)
                            continue;
                        CaptchaResult = rst.ToString();
                    }

                    Browser.Visible = false;

                    Form.Controls.Add(Message);

                    Browser.WaitForLoad();

                    HttpWebRequest Request = WebRequest.Create("https://www.tsumino.com/Read/AuthProcess") as HttpWebRequest;
                    Request.UserAgent = Tools.UserAgent;
                    Request.Method = "POST";
                    Request.ContentType = "application/x-www-form-urlencoded";

                    string PostString = $"Id={ID}&Page=1&g-recaptcha-response={CaptchaResult}";

                    byte[] Buffer = Encoding.UTF8.GetBytes(PostString);
                    Request.ContentLength = Buffer.LongLength;
                    var Stream = Request.GetRequestStream();
                    Stream.Write(Buffer, 0, Buffer.Length);
                    Stream.Close();

                    var Response = Request.GetResponse();
                    GetTokenFromHeaders(Response.Headers);
                    Response.Close();

                    Form.Close();
                }));

                TokenActivated = ReqToken(ID);                

            }

            if (string.IsNullOrWhiteSpace(Token) || !TokenActivated)
                throw new Exception();

            return Token;
        }

        private bool ReqToken(string ID) {
            var Request = (HttpWebRequest)WebRequest.Create($"https://www.tsumino.com/Read/View/{ID}/1");
            Request.Method = "HEAD";
            Request.UserAgent = Tools.UserAgent;
            var Response = Request.GetResponse();
            GetTokenFromHeaders(Request.Headers);
            bool TokenActivated = !Response.Headers.AllKeys.Contains("Location");
            Response.Close();

            return TokenActivated;
        }

        private void GetTokenFromHeaders(WebHeaderCollection Headers) {
            for (int i = 0; i < Headers.AllKeys.Count(); i++) {
                if (Headers.AllKeys[i].Trim().ToLower() == "set-cookie") {
                    string Cookie = Headers.Get(i);
                    if (!Cookie.Contains(CookieName + "="))
                        continue;

                    int Index = Cookie.IndexOf(CookieName + "=") + (CookieName.Length + 1);
                    int EndIndex = Cookie.IndexOf(";", Index);

                    Token = Cookie.Substring(Index, EndIndex - Index);
                    break;
                }
            }
        }

        Dictionary<string, string> NameMap = new Dictionary<string, string>();
        public string[] GetChapters() {
            string[] Elements = Main.GetElementsByAttribute(HTML, "class", "trow ", true);

            List<string> Links = new List<string>();
            if (Elements.Length == 0)
                Links.Add(MainPage);
            else {
                NameMap = new Dictionary<string, string>();
                foreach (string Element in Elements) {
                    string Link = Main.ExtractHtmlLinks(Element, "www.tsumino.com").First();
                    Links.Add(Link);
                    NameMap[Link] = (NameMap.Count() + 1).ToString();
                }
            }

            Links.Reverse(0, Links.Count());
            return Links.ToArray();
        }

        public string GetFullName() {
            string Element = Main.GetElementsByClasses(HTML, "book-title").First();

            string Name = Element.Split('>')[1].Split('/')[0].Split('<')[0].Trim();

            return HttpUtility.HtmlDecode(Name);
        }

        public string GetName(string CodedName) {
            int tmp;
            if (int.TryParse(CodedName, out tmp))
                CodedName = "Unk";

            return Main.GetRawNameFromUrlFolder(CodedName);
        }

        public string GetPosterUrl() {
            string Element = Main.GetElementsByClasses(HTML, "book-page-image", "img-responsive").First();

            string Link = Main.ExtractHtmlLinks(Element, "www.tsumino.com").First();

            return Link;
        }

        public void Initialize(string URL, out string Name, out string Page) {
            if (!IsValidLink(URL))
                throw new Exception();

            //https://www.tsumino.com/Book/Info/7691/genko-no-ori

            Name = GetName(URL.Split('/').Last());
            Page = URL;
        }

        public bool IsValidLink(string URL) {
            URL = URL.ToLower();
            return Uri.IsWellFormedUriString(URL, UriKind.Absolute) && URL.Contains("www.tsumino.com") && URL.Contains("/book/info/");
        }

        public void LoadPage(string URL) {
            HTML = Main.Download(URL, Encoding.UTF8);
            MainPage = URL;
        }

        public bool ValidateProxy(string Proxy) {
            throw new NotImplementedException();
        }
    }
}
