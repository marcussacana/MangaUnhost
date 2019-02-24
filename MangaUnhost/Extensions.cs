using Microsoft.Win32;
using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Web;
using System.Windows.Forms;

namespace MangaUnhost {
    static class Extensions {
        internal static void WaitForLoad(this WebBrowser Browser) {
            while (Browser.ReadyState != WebBrowserReadyState.Complete) {
                Application.DoEvents();
                System.Threading.Thread.Sleep(10);
            }
            Application.DoEvents();
        }

        internal static object Locker = new object();
        internal static void WaitForRedirect(this WebBrowser Browser) {
            bool Navigated = false;
            lock (Locker) {
                Browser.Navigated += (a, b) => { Navigated = true; };
                while (!Navigated) {
                    Application.DoEvents();
                    System.Threading.Thread.Sleep(10);
                }
            }
        }
        

        internal static string Get(this Cookie[] Cookies, string Name) {
            return (from x in Cookies where x.Name == Name select x.Value).First();
        }

        internal static Cookie[] GetCookies(this WebBrowser _Browser) {
            List<Cookie> Result = new List<Cookie>();
            List<string> Cookies = new List<string>();
            foreach (string Part in GetGlobalCookies(_Browser.Url.AbsoluteUri).Split(';')) {
                if (Part.ToLower().StartsWith(" path="))
                    continue;
                if (Part.ToLower().StartsWith(" domain="))
                    continue;
                if (Part.ToLower().StartsWith(" port="))
                    continue;
                if (!Part.Contains("="))
                    continue;

                Result.Add(new Cookie(Part.Split('=')[0].Trim(), Part.Split('=')[1]));
            }

            return Result.ToArray();
        }

        //Can trigger shit antivirus
        [DllImport("wininet.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern bool InternetGetCookieEx(string pchURL, string pchCookieName, StringBuilder pchCookieData, ref uint pcchCookieData, int dwFlags, IntPtr lpReserved);
        const int INTERNET_COOKIE_HTTPONLY = 0x00002000;

         static string GetGlobalCookies(string uri) {
            uint datasize = 1024;
            StringBuilder cookieData = new StringBuilder((int)datasize);
            if (InternetGetCookieEx(uri, null, cookieData, ref datasize, INTERNET_COOKIE_HTTPONLY, IntPtr.Zero) && cookieData.Length > 0) {
                return cookieData.ToString();
            } else {
                return null;
            }
        }

        internal static object InjectAndRunScript(this WebBrowser Browser, string Javascript, bool Eval = false) {
            Application.DoEvents();
            if (Eval)
                return Browser.Document.InvokeScript("eval", new object[] { Javascript });
            

            HtmlDocument Doc = Browser.Document;
            HtmlElement Head = Doc.GetElementsByTagName("head")[0];
            HtmlElement Script = Doc.CreateElement("script");

            string Func = $"_inj{new Random().Next(0, int.MaxValue)}";
            Script.SetAttribute("text", $"function {Func}() {{ {Javascript} }}");
            Head.AppendChild(Script);
            object ret = Browser.Document.InvokeScript(Func);
            Application.DoEvents();

            return ret;
        }

        
        internal static object ExecuteJavascript(this string Javascript, bool Eval = false) {
            if (Main.Instance.InvokeRequired)
                return Main.Instance.Invoke(new MethodInvoker(() => Javascript.ExecuteJavascript(Eval)));

            using (WebBrowser Browser = new WebBrowser()) {
                Browser.ScriptErrorsSuppressed = true;
                Browser.Navigate("about:blank");
                Browser.WaitForLoad();
                return Browser.InjectAndRunScript(Javascript);
            }
        }

        internal static string Eval(this WebBrowser Browser, string Script) {
            HtmlDocument Doc = Browser.Document;
            HtmlElement Body = Doc.GetElementsByTagName("body")[0];
            HtmlElement Div = Doc.CreateElement("div");
            string ID = $"_rid{new Random().Next(0, int.MaxValue)}";
            Div.SetAttribute("visible", "false");
            Div.SetAttribute("id", ID);
            Body.AppendChild(Div);

            string JS = $"document.getElementById('{ID}').innerHTML = eval(\"{Script.ToLiteral()}\");";

            Browser.Document.InvokeScript("eval", new object[] { JS });
            Application.DoEvents();
            Div = Browser.Document.GetElementById(ID);
            return Div.InnerHtml;
        }

        internal static void SetCookie(this WebBrowser Browser, string CookieName, string CookieValue) {
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
            foreach (var Line in Scr) {
                Script += Line + "\n";
            }

            Browser.InjectAndRunScript(Script);
        }

        internal static string GetCookie(this WebBrowser Browser, string CookieName) {
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
            foreach (var Line in Scr){
                Script += Line + "\n";
            }
            
            Browser.InjectAndRunScript(Script);
            Div = Browser.Document.GetElementById(ID);

            if (Div.InnerHtml == "Cookie Not Found")
                return null;

            return Div.InnerHtml;
        }


        delegate object MethodInvoker();
        internal static string Beautifier(this string Script) {
            if (Main.Instance.InvokeRequired)
                return (string)Main.Instance.Invoke(new MethodInvoker(() => Script.Beautifier()));

            EnsureBrowserEmulationEnabled();

            WebBrowser Browser = new WebBrowser();
            Browser.ScriptErrorsSuppressed = true;
            Browser.Navigate("https://beautifier.io");
            Browser.WaitForLoad();

            string JS = $"the.editor.setValue(\"{Script.ToLiteral()}\");beautify();the.editor.getValue();";

            return HttpUtility.HtmlDecode(Browser.Eval(JS));
        }

        internal static CookieContainer ToContainer(this Cookie Cookie) => new Cookie[] { Cookie }.ToContainer();
        internal static CookieContainer ToContainer(this Cookie[] Cookies) {
            CookieContainer Container = new CookieContainer();
            foreach (var Cookie in Cookies)
                Container.Add(Cookie);

            return Container;
        }

        internal static string ToLiteral(this string String, bool Quote = true, bool Apostrophe = false) {
            string Result = string.Empty;
            foreach (char c in String) {
                switch (c) {
                    case '\n':
                        Result += "\\n";
                        break;
                    case '\\':
                        Result += "\\\\";
                        break;
                    case '\t':
                        Result += "\\t";
                        break;
                    case '\r':
                        Result += "\\r";
                        break;
                    case '"':
                        if (!Quote)
                            goto default;
                        Result += "\\\"";
                        break;
                    case '\'':
                        if (!Apostrophe)
                            goto default;
                        Result += "\\'";
                        break;
                    default:
                        Result += c;
                        break;
                }
            }

            return Result;
        }

        internal static void EnsureBrowserEmulationEnabled(bool Uninstall = false) {
            try {
                using (
                    var rk = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Internet Explorer\Main\FeatureControl\FEATURE_BROWSER_EMULATION", true)
                ) {
                    if (!Uninstall) {
                        dynamic value = rk.GetValue(Path.GetFileName(Application.ExecutablePath));
                        if (value == null)
                            rk.SetValue(Path.GetFileName(Application.ExecutablePath), (uint)11001, RegistryValueKind.DWord);
                    } else
                        rk.DeleteValue(Path.GetFileName(Application.ExecutablePath));
                }
            } catch {
            }
        }
    }
}
