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
        public string HostName {
            get {
                return "Tsumino";
            }
        }

        public string DemoUrl {
            get {
                return "https://www.tsumino.com/Book/Info/7691/genko-no-ori";
            }
        }

        string HTML;
        string Token;
        string LastToken;
        string MainPage;

        const string CookieName = "ASP.NET_SessionId";
        const string USERAGENT = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/65.0.3325.181 Safari/537.36 OPR/52.0.2871.64";
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

            string Info = RequestInfo(ID);

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
                Request.UserAgent = USERAGENT;
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
            } catch (Exception ex){
                if (Token == null)
                    throw ex;
                LastToken = Token;
                Token = null;
                RequestInfo(ID);
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
                        Text = "Resolva o Captcha",
                        FormBorderStyle = FormBorderStyle.FixedToolWindow
                    };
                    var Browser = new WebBrowser() {
                        Dock = DockStyle.Fill,
                        ScriptErrorsSuppressed = true,
                        IsWebBrowserContextMenuEnabled = false,
                        AllowWebBrowserDrop = false
                    };
                    var Message = new Label() {
                        Text = "Processando...",
                        Font = new System.Drawing.Font("Consola", 24),
                        Dock = DockStyle.Fill,
                        TextAlign = System.Drawing.ContentAlignment.MiddleCenter
                    };

                    Form.Controls.Add(Browser);

                    Browser.Navigate($"https://www.tsumino.com/Read/Auth/{ID}");
                    Browser.WaitForLoad();
                    Form.Show(Main);

                    Again:;
                    try {
                        while (Browser.Url.AbsoluteUri.ToLower().Contains("read/auth")) {
                            Application.DoEvents();
                            System.Threading.Thread.Sleep(10);
                        }
                    } catch {
                        Form.Show(Main);
                        goto Again;
                    }

                    Browser.Visible = false;


                    Form.Controls.Add(Message);

                    Browser.WaitForLoad();

                    Form.Close();
                }));

                TokenActivated = ReqToken(ID);

                if (string.IsNullOrEmpty(Token) && !string.IsNullOrEmpty(LastToken))
                    Token = LastToken;

            }

            if (string.IsNullOrWhiteSpace(Token) || !TokenActivated)
                throw new Exception();

            return Token;
        }

        private bool ReqToken(string ID) {
            var Request = (HttpWebRequest)WebRequest.Create($"https://www.tsumino.com/Read/View/{ID}/1");
            Request.Method = "HEAD";
            Request.UserAgent = USERAGENT;
            var Response = Request.GetResponse();
            for (int i = 0; i < Response.Headers.AllKeys.Count(); i++) {
                if (Response.Headers.AllKeys[i].Trim().ToLower() == "set-cookie") {
                    string Cookie = Response.Headers.Get(i);
                    if (!Cookie.Contains(CookieName + "="))
                        continue;

                    int Index = Cookie.IndexOf(CookieName + "=") + (CookieName.Length + 1);
                    int EndIndex = Cookie.IndexOf(";", Index);

                    Token = Cookie.Substring(Index, EndIndex - Index);
                    break;
                }
            }
            bool TokenActivated = !Response.Headers.AllKeys.Contains("Location");
            Response.Close();

            return TokenActivated;
        }

        Dictionary<string, string> NameMap = new Dictionary<string, string>();
        public string[] GetChapters() {
            string[] Elements = Main.GetElementsByAttribute(HTML, "class", "trow ", true);

            List<string> Links = new List<string>();
            if (Elements.Length == 0)
                Links.Add(MainPage);
            else {
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
            string Element = Main.GetElementsByClasses(HTML, 0, "book-title").First();

            string Name = Element.Split('>')[1].Split('/')[0];

            return Name;
        }

        public string GetName(string CodedName) {
            int tmp;
            if (int.TryParse(CodedName, out tmp))
                CodedName = "Unk";

            return Main.GetRawNameFromUrlFolder(CodedName);
        }

        public string GetPosterUrl() {
            string Element = Main.GetElementsByClasses(HTML, 0, "book-page-image", "img-responsive").First();

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
    }
}
