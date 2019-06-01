using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Web;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace MangaUnhost {
    static class Extensions {
        internal delegate object MethodInvoker();
        internal static string Beautifier(this string Script) {
            if (Main.Instance.InvokeRequired)
                return (string)Main.Instance.Invoke(new MethodInvoker(() => Script.Beautifier()));

            MangaUnhost.Browser.EnsureBrowserEmulationEnabled();

            WebBrowser Browser = MangaUnhost.Browser.Create();
            Browser.Navigate("https://beautifier.io");
            Browser.WaitForLoad();

            string JS = $"the.editor.setValue(\"{Script.ToLiteral()}\");beautify();the.editor.getValue();";

            return HttpUtility.HtmlDecode(Browser.Eval(JS));
        }

        internal static CookieContainer ToContainer(this Cookie Cookie) => new Cookie[] { Cookie }.ToContainer();
        internal static CookieContainer ToContainer(this Cookie[] Cookies) {
            CookieContainer Container = new CookieContainer();
            foreach (var Cookie in Cookies)
                if (Cookie != null)
                    try
                    {
                        Container.Add(Cookie);
                    }
                    catch { }

            return Container;
        }

        internal static string Between(this string String, char Begin, char End, int IndexA = 0, int IndexB = 0) => String.Split(Begin)[IndexA + 1].Split(End)[IndexB];
   
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
        internal static int EndIndexOf(this string Origin, string Content, int BeginIndex = 0)
        {
            int Val = Origin.IndexOf(Content, BeginIndex);
            if (Val == -1)
                return -1;

            return Val + Content.Length;
        }   

        internal static string Substring(this string String, string AfterOf, bool CaseSensitive = false)
        {
            return String.Substring((CaseSensitive ? String.EndIndexOf(AfterOf) : String.ToLower().EndIndexOf(AfterOf.ToLower())));
        }
        internal static string Substring(this string String, string AfterOf, string BeforeOf, bool CaseSensitive = false)
        {
            string Result = String.Substring((CaseSensitive ? String.EndIndexOf(AfterOf) : String.ToLower().EndIndexOf(AfterOf.ToLower())));

            return Result.Substring(0, (CaseSensitive ? Result.IndexOf(BeforeOf) : Result.ToLower().IndexOf(BeforeOf.ToLower())));
        }

        internal static void AddRange<Key, Value>(this Dictionary<Key, Value> Dictionary, Key[] Keys, Value[] Values)
        {
            if (Keys.Length != Values.Length)
                throw new IndexOutOfRangeException();

            for (int i = 0; i < Keys.Length; i++)
                Dictionary.Add(Keys[i], Values[i]);
        }

        internal static bool IsCloudflareTriggered(this WebBrowser Browser) => Browser.DocumentText.IsCloudflareTriggered();
        internal static bool IsCloudflareTriggered(this string HTML) => HTML.Contains("5 seconds...") || HTML.Contains("Checking your browser");
        internal static string GetUserAgent(this WebBrowser Browser) => (string)Browser.InjectAndRunScript("return clientInformation.userAgent;");

        internal static T GetRandomElement<T>(this T[] Array) {
            return Array[new Random().Next(0, Array.Length)];
        }

        internal static string JsonEncode<T>(T Data) {
            return new JavaScriptSerializer().Serialize(Data);
        }
        internal static T JsonDecode<T>(string Json) {
            return (T)new JavaScriptSerializer().Deserialize(Json, typeof(T));
        }
    }
}
