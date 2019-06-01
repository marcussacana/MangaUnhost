using Microsoft.Win32;
using mshtml;
using SHDocVw;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace MangaUnhost
{
    internal static class Browser
    {
        internal static System.Windows.Forms.WebBrowser Create() => (System.Windows.Forms.WebBrowser)Main.Instance.Invoke(
            new MethodInvoker(() => new System.Windows.Forms.WebBrowser()
            {
                Dock = DockStyle.Fill,
                ScriptErrorsSuppressed = true,
                IsWebBrowserContextMenuEnabled = false,
                AllowWebBrowserDrop = false,
                Visible = false
            }));

        internal static void WaitForLoad(this System.Windows.Forms.WebBrowser Browser)
        {
            if (Browser.InvokeRequired)
            {
                Browser.Invoke(new MethodInvoker(() => { Browser.WaitForLoad(); return null; }));
                return;
            }

            while (Browser.ReadyState != WebBrowserReadyState.Complete)
            {
                Application.DoEvents();
                System.Threading.Thread.Sleep(10);
            }
            Application.DoEvents();
        }

        internal static string GetHtml(this System.Windows.Forms.WebBrowser Browser)
        {
            if (Browser.InvokeRequired)
                return (string)Browser.Invoke(new MethodInvoker(() => Browser.GetHtml()));

            //var Doc = Browser.Document.Window.GetDocument();

            return Browser.Document.Body.Parent.OuterHtml;
        }

        internal static void Sleep(this System.Windows.Forms.WebBrowser Browser, int Seconds = 3, int Mileseconds = 0)
        {
            if (Browser.InvokeRequired)
            {
                Browser.Invoke(new MethodInvoker(() => { Browser.Sleep(Seconds, Mileseconds); return null; }));
                return;
            }

            DateTime Finish = DateTime.Now.AddSeconds(Seconds);
            Finish.AddMilliseconds(Mileseconds);
            while (DateTime.Now <= Finish)
            {
                Application.DoEvents();
                System.Threading.Thread.Sleep(5);
            }
        }

        internal static void AsyncNavigate(this System.Windows.Forms.WebBrowser Browser, string URL)
        {
            if (Browser.InvokeRequired)
            {
                Browser.Invoke(new MethodInvoker(() => { Browser.AsyncNavigate(URL); return null; }));
                return;
            }

            Browser.Navigate(URL);
        }

        internal static object Locker = new object();
        internal static void WaitForRedirect(this System.Windows.Forms.WebBrowser Browser)
        {
            if (Browser.InvokeRequired)
            {
                Browser.Invoke(new MethodInvoker(() => { Browser.WaitForRedirect(); return null; }));
                return;
            }

            bool Navigated = false;
            lock (Locker)
            {
                Browser.Navigated += (a, b) => { Navigated = true; };
                while (!Navigated)
                {
                    Application.DoEvents();
                    System.Threading.Thread.Sleep(10);
                }
            }
        }


        internal static string Get(this Cookie[] Cookies, string Name)
        {
            return (from x in Cookies where x.Name == Name select x.Value).First();
        }

        internal static Cookie[] GetCookies(this System.Windows.Forms.WebBrowser Browser, string ForceDomain = null)
        {
            List<Cookie> Result = new List<Cookie>();
            foreach (string Part in GetGlobalCookies(Browser.Url.AbsoluteUri).Split(';'))
            {
                if (Part.ToLower().StartsWith(" path="))
                    continue;
                if (Part.ToLower().StartsWith(" domain="))
                {
                    string Domain = Part.Split('=')[1];
                    Cookie Cookie = Result.Last();
                    if (!string.IsNullOrWhiteSpace(Cookie.Domain))
                    {
                        Result.RemoveAt(Result.Count - 1);
                        Cookie = new Cookie(Cookie.Name, Cookie.Value, "/", Domain);
                        Result.Add(Cookie);
                    }
                    continue;
                }
                if (Part.ToLower().StartsWith(" port="))
                    continue;
                if (!Part.Contains("="))
                    continue;

                Cookie Output;
                if (ForceDomain == null)
                    Output = new Cookie(Part.Split('=')[0].Trim(), Part.Split('=')[1], "/", Browser.Url.DnsSafeHost.Replace("www.", ""));
                else
                    Output = new Cookie(Part.Split('=')[0].Trim(), Part.Split('=')[1], "/", ForceDomain);

                Result.Add(Output);
            }

            return Result.ToArray();
        }

        //Can trigger shit antivirus
        [DllImport("wininet.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern bool InternetGetCookieEx(string pchURL, string pchCookieName, StringBuilder pchCookieData, ref uint pcchCookieData, int dwFlags, IntPtr lpReserved);
        const int INTERNET_COOKIE_HTTPONLY = 0x00002000;

        static string GetGlobalCookies(string uri)
        {
            uint datasize = 1024;
            StringBuilder cookieData = new StringBuilder((int)datasize);
            if (InternetGetCookieEx(uri, null, cookieData, ref datasize, INTERNET_COOKIE_HTTPONLY, IntPtr.Zero) && cookieData.Length > 0)
            {
                return cookieData.ToString();
            }
            else
            {
                return null;
            }
        }

        internal static object InjectAndRunScript(this HtmlDocument Document, string Javascript, bool Eval = false)
        {
            if (Main.Instance.InvokeRequired)
                return Main.Instance.Invoke(new MethodInvoker(() => { return Document.InjectAndRunScript(Javascript, Eval); }));

            Application.DoEvents();
            if (Eval)
                return Document.InvokeScript("eval", new object[] { Javascript });

            HtmlElement Head = Document.GetElementsByTagName("head")[0];
            HtmlElement Script = Document.CreateElement("script");

            string Func = $"_inj{new Random().Next(0, int.MaxValue)}";
            Script.SetAttribute("text", $"function {Func}() {{ {Javascript} }}");
            Head.AppendChild(Script);
            object ret = Document.InvokeScript(Func);
            Application.DoEvents();

            return ret;
        }
        internal static object InjectAndRunScript(this System.Windows.Forms.WebBrowser Browser, string Javascript, bool Eval = false)
        {
            if (Browser.InvokeRequired)
                return Browser.Invoke(new MethodInvoker(() => Browser.InjectAndRunScript(Javascript, Eval)));

            return InjectAndRunScript(Browser.Document, Javascript, Eval);
        }


        internal static object ExecuteJavascript(this string Javascript, bool Eval = false)
        {
            if (Main.Instance.InvokeRequired)
                return Main.Instance.Invoke(new MethodInvoker(() => Javascript.ExecuteJavascript(Eval)));

            using (var Browser = new System.Windows.Forms.WebBrowser())
            {
                Browser.ScriptErrorsSuppressed = true;
                Browser.Navigate("about:blank");
                Browser.WaitForLoad();
                return Browser.InjectAndRunScript(Javascript);
            }
        }

        internal static string Eval(this HtmlDocument Document, string Script)
        {
            if (Main.Instance.InvokeRequired)
                return (string)Main.Instance.Invoke(new MethodInvoker(() => { return Document.Eval(Script); }));

            HtmlElement Body = Document.GetElementsByTagName("body")[0];
            HtmlElement Div = Document.CreateElement("div");
            string ID = $"_rid{new Random().Next(0, int.MaxValue)}";
            Div.SetAttribute("visible", "false");
            Div.SetAttribute("id", ID);
            Body.AppendChild(Div);

            string JS = $"document.getElementById('{ID}').innerHTML = eval(\"{Script.ToLiteral()}\");";

            Document.InvokeScript("eval", new object[] { JS });
            Application.DoEvents();
            Div = Document.GetElementById(ID);
            return Div.InnerHtml;
        }
        internal static string Eval(this System.Windows.Forms.WebBrowser Browser, string Script)
        {
            if (Browser.InvokeRequired)
                return (string)Browser.Invoke(new MethodInvoker(() => Browser.Eval(Script)));
            return Eval(Browser.Document, Script);
        }

        internal static void SetCookie(this System.Windows.Forms.WebBrowser Browser, string CookieName, string CookieValue)
        {
            var Scr = new string[] {
                $"var name = '{CookieName}';",
                $"var value = '{CookieValue}';",
                "var expires = '';",
                "if (days) {",
                "    var date = new Date();",
                "    date.setTime(date.getTime() + (days * 24 * 60 * 60 * 1000));",
                "    expires = '; expires=' + date.toUTCString();",
                "}",
                "document.cookie = name + '=' + (value || '') + expires + '; path=/';"
            };

            string Script = string.Empty;
            foreach (var Line in Scr)
            {
                Script += Line + "\n";
            }

            Browser.InjectAndRunScript(Script);
        }

        internal static string GetCookie(this System.Windows.Forms.WebBrowser Browser, string CookieName)
        {
            HtmlDocument Doc = Browser.Document;
            HtmlElement Body = Doc.GetElementsByTagName("body")[0];
            HtmlElement Div = Doc.CreateElement("div");
            string ID = $"_rid{new Random().Next(0, int.MaxValue)}";
            Div.SetAttribute("visible", "false");
            Div.SetAttribute("id", ID);
            Body.AppendChild(Div);

            const string FailMsg = "Cookie Not Found";
            var Scr = new string[] {
                $"var nameEQ = '{CookieName}=';",
                "var ca = document.cookie.split(';');",
                $"var rst = '{FailMsg}';",
                "for(var i=0;i < ca.length;i++) {",
                "    var c = ca[i];",
                "    while (c.charAt(0)==' ')",
                "		c = c.substring(1,c.length);",
                "    if (c.indexOf(nameEQ) == 0)",
                "		rst = c.substring(nameEQ.length,c.length);",
                "}",
                $"document.getElementById('{ID}').innerHTML = rst;"
            };

            string Script = string.Empty;
            foreach (var Line in Scr)
            {
                Script += Line + "\n";
            }

            Browser.InjectAndRunScript(Script);
            Div = Browser.Document.GetElementById(ID);

            if (Div.InnerHtml == "Cookie Not Found")
                return null;

            return Div.InnerHtml;
        }

        internal static int CaptchaFails = 0;
        internal static void SolveCaptcha(this System.Windows.Forms.WebBrowser Browser)
        {
            //Feature Not Stable Yet
            if (!Program.Debug)
                return;

            if (Browser.InvokeRequired)
            {
                Browser.Invoke(new MethodInvoker(() => { Browser.SolveCaptcha(); return null; }));
                return;
            }

            //This Captcha solve works, but the google detect it after a certain amout of usages,
            //After detected you need wait a certain time to try again.
            if (CaptchaFails > 3)
                return;

            try
            {
                //recaptcha/api2/anchor
                //recaptcha/api2/bframe

                Browser.WaitForLoad();
                int Tries = 2;
                while (Tries > 0)
                {
                    var Anchor = Browser.GetFrameByUriPath("recaptcha/api2/anchor");
                    if (Anchor != null)
                    {
                        Anchor.InjectAndRunScript("document.getElementsByClassName(\"rc-anchor-content\")[0].click();");
                        Browser.Sleep(4);

                        const string GetErrorJS = "return document.getElementsByClassName(\"rc-audiochallenge-error-message\")[0].innerHTML;";
                        const string GetAudioJS = "return document.getElementsByClassName(\"rc-audiochallenge-tdownload-link\")[0].href;";

                        var Challenge = Browser.GetFrameByUriPath("recaptcha/api2/bframe");

                        if (Challenge != null)
                        {

                            int ATries = 3;
                            while (ATries > 0)
                            {
                                string AudioLink = (string)Challenge.InjectAndRunScript(GetAudioJS);
                                if (string.IsNullOrWhiteSpace(AudioLink))
                                {
                                    Challenge.InjectAndRunScript("document.getElementById(\"recaptcha-audio-button\").click();");
                                    Browser.Sleep();
                                }
                                AudioLink = (string)Challenge.InjectAndRunScript(GetAudioJS);
                                if (!string.IsNullOrWhiteSpace(AudioLink))
                                {
                                    byte[] MP3 = Main.Download(AudioLink, Referrer: "www.google.com", UserAgent: Browser.GetUserAgent(), Cookies: Browser.GetCookies().ToContainer());
                                    byte[] WAV = Tools.Mp3ToWav(MP3);

                                    string Captcha = Tools.GetTextFromSpeech(WAV);
                                    foreach (char c in Captcha)
                                    {
                                        Challenge.InjectAndRunScript($"document.getElementById(\"audio-response\").value += \"{c}\";");
                                        Browser.Sleep(0, new Random().Next(80, 100));
                                    }

                                    Challenge.InjectAndRunScript("document.getElementById(\"recaptcha-verify-button\").click();");
                                    Browser.Sleep(2);

                                    string ERROR = (string)Challenge.InjectAndRunScript(GetErrorJS);
                                    if (ERROR == null || ERROR.Length == 0)
                                        return;
                                }
                                else
                                {
                                    Browser.InjectAndRunScript("grecaptcha.reset();");
                                    Browser.Sleep();
                                    break;
                                }
                                ATries--;
                            }
                            if (ATries <= 0)
                            {
                                CaptchaFails++;
                                Browser.InjectAndRunScript("grecaptcha.reset();");
                                return;
                            }
                        }
                    }

                    Tries--;
                }
            }
            catch (Exception ex){

            }
        }

        internal static bool CaptchaSolved(this System.Windows.Forms.WebBrowser Browser)
        {
            if (Browser.InvokeRequired)
            {
                return (bool)Browser.Invoke(new MethodInvoker(() => { return Browser.CaptchaSolved(); }));
            }
            try
            {
                object rst = Browser.InjectAndRunScript("return grecaptcha.getResponse();");
                if (rst != null)
                    return true;
            }
            catch { }
            return false;
        }

        internal static HtmlDocument GetFrameByUriPath(this System.Windows.Forms.WebBrowser Browser, string PathPrefix)
        {
            foreach (var Window in Browser.Document.Window.Frames.Cast<HtmlWindow>())
            {
                var Frame = Window.GetDocument();
                if (!Frame.Url.LocalPath.ToLower().Trim('/').StartsWith(PathPrefix.ToLower().Trim('/')))
                    continue;
                return Frame;
            }

            return null;
        }

        internal delegate object MethodInvoker();
        internal static void EnsureBrowserEmulationEnabled(bool Uninstall = false)
        {
            try
            {
                using (
                    var rk = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Internet Explorer\Main\FeatureControl\FEATURE_BROWSER_EMULATION", true)
                )
                {
                    if (!Uninstall)
                    {
                        dynamic value = rk.GetValue(Path.GetFileName(Application.ExecutablePath));
                        if (value == null)
                            rk.SetValue(Path.GetFileName(Application.ExecutablePath), (uint)11001, RegistryValueKind.DWord);
                    }
                    else
                        rk.DeleteValue(Path.GetFileName(Application.ExecutablePath));
                }
            }
            catch
            {
            }
        }




        private static FieldInfo ShimManager = typeof(HtmlWindow).GetField("shimManager", BindingFlags.NonPublic | BindingFlags.Instance);
        private static ConstructorInfo HtmlDocumentCtor = typeof(HtmlDocument).GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance)[0];

        public static HtmlDocument GetDocument(this HtmlWindow window)
        {
            var rawDocument = (window.DomWindow as IHTMLWindow2).GetDocumentFromWindow();

            var shimManager = ShimManager.GetValue(window);

            var htmlDocument = HtmlDocumentCtor
                .Invoke(new[] { shimManager, rawDocument }) as HtmlDocument;

            return htmlDocument;
        }


        // Returns null in case of failure.
        public static IHTMLDocument2 GetDocumentFromWindow(this IHTMLWindow2 htmlWindow)
        {
            if (htmlWindow == null)
            {
                return null;
            }

            // First try the usual way to get the document.
            try
            {
                IHTMLDocument2 doc = htmlWindow.document;

                return doc;
            }
            catch (COMException comEx)
            {
                // I think COMException won't be ever fired but just to be sure ...
                if (comEx.ErrorCode != E_ACCESSDENIED)
                {
                    return null;
                }
            }
            catch (System.UnauthorizedAccessException)
            {
            }
            catch
            {
                // Any other error.
                return null;
            }

            // At this point the error was E_ACCESSDENIED because the frame contains a document from another domain.
            // IE tries to prevent a cross frame scripting security issue.
            try
            {
                // Convert IHTMLWindow2 to IWebBrowser2 using IServiceProvider.
                IServiceProvider sp = (IServiceProvider)htmlWindow;

                // Use IServiceProvider.QueryService to get IWebBrowser2 object.
                Object brws = null;
                sp.QueryService(ref IID_IWebBrowserApp, ref IID_IWebBrowser2, out brws);

                // Get the document from IWebBrowser2.
                IWebBrowser2 browser = (IWebBrowser2)(brws);

                return (IHTMLDocument2)browser.Document;
            }
            catch
            {
            }

            return null;
        }

        private const int E_ACCESSDENIED = unchecked((int)0x80070005L);
        private static Guid IID_IWebBrowserApp = new Guid("0002DF05-0000-0000-C000-000000000046");
        private static Guid IID_IWebBrowser2 = new Guid("D30C1661-CDAF-11D0-8A3E-00C04FC9E26E");
    }

    // This is the COM IServiceProvider interface, not System.IServiceProvider .Net interface!
    [ComImport(), ComVisible(true), Guid("6D5140C1-7436-11CE-8034-00AA006009FA"),
    InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IServiceProvider
    {
        [return: MarshalAs(UnmanagedType.I4)]
        [PreserveSig]
        int QueryService(ref Guid guidService, ref Guid riid, [MarshalAs(UnmanagedType.Interface)] out object ppvObject);
    }
}
