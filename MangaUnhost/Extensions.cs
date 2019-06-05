using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace MangaUnhost {
    static class Extensions
    {

        internal static void Sleep(this Control Control, int Seconds = -1, int Mileseconds = 0)
        {
            if (Control.InvokeRequired)
            {
                Control.Invoke(new MethodInvoker(() => { Control.Sleep(Seconds, Mileseconds); return null; }));
                return;
            }

            DateTime Finish = DateTime.Now;
            if (Mileseconds == 0)
                Finish = Finish.AddSeconds(Seconds == -1 ? 3 : Seconds);
            else
                Finish = Finish.AddMilliseconds(Mileseconds);
            while (DateTime.Now <= Finish)
            {
                Application.DoEvents();

                if (Mileseconds == 0)
                    System.Threading.Thread.Sleep(5);
            }
        }

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

        internal static void WaitForExit(this Thread Thread)
        {
            while (Thread.IsRunning())
            {
                Application.DoEvents();
                Thread.Sleep(100);
            }
        }
        internal static bool IsRunning(this Thread Thread)
        {
            return Thread.ThreadState == ThreadState.Running || Thread.ThreadState == ThreadState.Background
                || Thread.ThreadState == ThreadState.WaitSleepJoin;
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
        internal static bool IsCloudflareTriggered(this string HTML) => HTML.Contains("5 seconds...") || HTML.Contains("Checking your browser") || HTML.Contains("why_captcha_headline");
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

        internal static void PostClick(this Control Control, Point Point)
        {
            PostMessage(Control.Handle, WMessages.WM_MOUSEMOVE, 0, Point.Rand().ToInt32());
            Control.Sleep(Mileseconds: 10);
            PostMessage(Control.Handle, WMessages.WM_MOUSEMOVE, 0, Point.ToInt32());
            Control.Sleep(Mileseconds: new Random().Next(50, 100));
            PostMessage(Control.Handle, WMessages.WM_LBUTTONDOWN, MK_LBUTTON, Point.ToInt32());
            Control.Sleep(Mileseconds: new Random().Next(100, 200));
            PostMessage(Control.Handle, WMessages.WM_LBUTTONUP, 0, Point.ToInt32());
            Control.Sleep(Mileseconds: new Random().Next(100, 150));
            PostMessage(Control.Handle, WMessages.WM_MOUSEMOVE, 0, Point.Rand().ToInt32());
        }

        [DllImport("user32.dll")]
        static extern bool PostMessage(IntPtr hWnd, WMessages Msg, int wParam, int lParam);

        public enum WMessages : uint
        {
            WM_LBUTTONDOWN = 0x201,
            WM_LBUTTONUP = 0x202,
            WM_LBUTTONDBLCLK = 0x203,
            WM_RBUTTONDOWN = 0x204,
            WM_RBUTTONUP = 0x205,
            WM_RBUTTONDBLCLK = 0x206,
            WM_MOUSEMOVE = 0x0200
        }

        const int MK_LBUTTON = 1;


        static Point Rand(this Point Point) => new Point(Point.X + new Random().Next(0, 5), Point.Y + new Random().Next(0, 5));
        static int ToInt32(this Point Point) => (Point.X << 16) | (Point.Y & 0xFFFF);
    }
}
