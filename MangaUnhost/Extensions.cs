using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace MangaUnhost {
    static class Extensions {
        internal static void WaitForLoad(this WebBrowser Browser) {
            while (Browser.ReadyState != WebBrowserReadyState.Complete) {
                Application.DoEvents();
                System.Threading.Thread.Sleep(50);
            }
            Application.DoEvents();
        }

        internal static void InjectAndRunScript(this WebBrowser Browser, string Javascript) {
            Application.DoEvents();
            HtmlDocument Doc = Browser.Document;
            HtmlElement Head = Doc.GetElementsByTagName("head")[0];
            HtmlElement Script = Doc.CreateElement("script");
            string Func = $"_inj{new Random().Next(0, int.MaxValue)}";
            Script.SetAttribute("text", $"function {Func}() {{ {Javascript} }}");
            Head.AppendChild(Script);
            Browser.Document.InvokeScript(Func);
            Application.DoEvents();
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
                $"document.getElementById('{ID}').innerHTML = rst;",
            };

            string Script = string.Empty;
            foreach (var Line in Scr){
                Script += Line;
            }
            
            Browser.InjectAndRunScript(Script);

            Div = Browser.Document.GetElementById(ID);

            if (Div.InnerHtml == "Cookie Not Found")
                return null;

            return Div.InnerHtml;
        }
    }
}
