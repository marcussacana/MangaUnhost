using CefSharp;
using CefSharp.DevTools.Debugger;
using CefSharp.EventHandler;
using CefSharp.Handler;
using CefSharp.OffScreen;
using HtmlAgilityPack;
using MangaUnhost;
using MangaUnhost.Others;
using Nito.AsyncEx;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Windows.Forms;
using HtmlDocument = HtmlAgilityPack.HtmlDocument;

namespace MangaUnhost.Browser
{
    public static class JSTools
    {

        static ChromiumWebBrowser _DefBrowser = null;
        public static ChromiumWebBrowser DefaultBrowser
        {
            get
            {
                if (_DefBrowser != null)
                    return _DefBrowser;

                _DefBrowser = new ChromiumWebBrowser("about:blank");
                _DefBrowser.ReCaptchaHook();
                _DefBrowser.SetUserAgent(ProxyTools.UserAgent);

                while (!DefaultBrowser.IsBrowserInitialized)
                {
                    ThreadTools.Wait(50, true);
                }

                return _DefBrowser;
            }
        }

        public static object EvaluateScript(string Script)
        {
            DefaultBrowser.GetBrowser().WaitForLoad();

            return DefaultBrowser.EvaluateScript(Script);
        }
        public static T EvaluateScript<T>(string Script)
        {
            DefaultBrowser.GetBrowser().WaitForLoad();

            return DefaultBrowser.EvaluateScript<T>(Script);
        }

        public static object EvaluateScript(string Script, bool Reload)
        {
            var Rst = EvaluateScript(Script);
            if (Reload)
                EvaluateScript("location.reload()");
            return Rst;
        }

        public static T EvaluateScript<T>(string Script, bool Reload)
        {
            var Rst = EvaluateScript<T>(Script);
            if (Reload)
                EvaluateScript("location.reload()");
            return Rst;
        }

        public static void EarlyInjection(this IWebBrowser Browser, string Javascript, JavascriptInjectionFilter.Locations Location = JavascriptInjectionFilter.Locations.HEAD)
        {
            Browser.DisableCORS();

            if (!(Browser.RequestHandler is RequestEventHandler))
                Browser.RequestHandler = new RequestEventHandler();

            ((RequestEventHandler)Browser.RequestHandler).OnResourceRequestEvent += (sender, args) =>
            {
                if (!(args.ResourceRequestHandler is ResourceRequestEventHandler))
                    args.ResourceRequestHandler = new ResourceRequestEventHandler();

                ((ResourceRequestEventHandler)args.ResourceRequestHandler).OnGetResourceResponseFilterEvent += (sender, args) =>
                {
                    if (!args.Frame.IsValid)
                        return;

                    if (args.Frame.IsMain && args.Request.ResourceType == ResourceType.MainFrame)
                    {
                        args.ResponseFilter = new JavascriptInjectionFilter(Javascript, Location);
                    }
                };
            };
        }

        public static void DisableCORS(this IWebBrowser Browser)
        {
            Browser.RegisterWebRequestHandlerEvents(null, (sender, args) =>
            {
                var HasCookies = args.WebRequest.Headers["Cookie"] != null;

                var Uri = args.WebRequest.RequestUri;
                var Origin = new Uri(args.WebRequest.Referer ?? args.WebRequest.Headers["Origin"] ?? args.WebRequest.RequestUri.AbsoluteUri);
                args.Headers["Access-Control-Allow-Origin"] = HasCookies ? $"{Uri.Scheme}://{Origin.Host}" : "*";
                args.Headers["Content-Security-Policy"] = "default-src * data: mediastream: blob: filesystem: about: ws: wss: 'unsafe-eval' 'wasm-unsafe-eval' 'unsafe-inline';";
            });
        }

        public static IEnumerable<IFrame> GetFrames(this IBrowser Browser)
        {
            foreach (var FrameID in Browser.GetFrameIdentifiers())
            {
                var Frame = Browser.GetFrameByIdentifier(FrameID);
                if (Frame != null && Frame.IsValid)
                    yield return Frame;
            }
        }
        public static void InjectXPATH(this ChromiumWebBrowser Browser) => Browser.GetBrowser().InjectXPATH();
        public static void InjectXPATH(this IBrowser Browser) => Browser.EvaluateScriptUnsafe(Properties.Resources.XPATHScript);
        public static T EvaluateScript<T>(this ChromiumWebBrowser Browser, string Script) => (T)Browser.GetBrowser().EvaluateScript(Script);
        public static T EvaluateScriptUnsafe<T>(this ChromiumWebBrowser Browser, string Script) => (T)Browser.GetBrowser().EvaluateScriptUnsafe(Script);
        public static T EvaluateScript<T>(this CefSharp.WinForms.ChromiumWebBrowser Browser, string Script) => (T)Browser.GetBrowser().EvaluateScript(Script);

        public static T EvaluateScript<T>(this IBrowser Browser, string Script) => (T)Browser.MainFrame.EvaluateScript(Script);
        public static T EvaluateScriptUnsafe<T>(this IBrowser Browser, string Script) => (T)Browser.MainFrame.EvaluateScriptUnsafe(Script);
        public static void EvaluateScript<T>(this ChromiumWebBrowser Browser, string Script, out T Result) => Result = (T)Browser.GetBrowser().EvaluateScript(Script);
        public static void EvaluateScript<T>(this IBrowser Browser, string Script, out T Result) => Result = (T)Browser.MainFrame.EvaluateScript(Script);
        public static void EvaluateScriptUnsafe<T>(this IBrowser Browser, string Script, out T Result) => Result = (T)Browser.MainFrame.EvaluateScriptUnsafe(Script);
        public static object EvaluateScript(this ChromiumWebBrowser Browser, string Script) => Browser.GetBrowser().EvaluateScript(Script);
        public static object EvaluateScriptUnsafe(this ChromiumWebBrowser Browser, string Script) => Browser.GetBrowser().EvaluateScriptUnsafe(Script);
        public static object EvaluateScript(this IBrowser Browser, string Script)
        {
            return Browser.MainFrame.EvaluateScript(Script);
        }
        public static object EvaluateScriptUnsafe(this IBrowser Browser, string Script)
        {
            return Browser.MainFrame.EvaluateScriptUnsafe(Script);
        }
        public static T EvaluateScript<T>(this IFrame Frame, string Script) => (T)Frame.EvaluateScript(Script);
        public static T EvaluateScriptUnsafe<T>(this IFrame Frame, string Script) => (T)Frame.EvaluateScriptUnsafe(Script);
        public static object EvaluateScript(this IFrame Frame, string Script)
        {
            if (Program.Debug)
                Program.Writer?.WriteLine("EVAL At: {0}\r\nScript: {1}", Frame.Url, Script);

            Frame.WaitForLoad();

            try
            {
                return Frame.EvaluateScriptAsync(Script).GetAwaiter().GetResult().Result;
            }
            catch
            {
                return null;
            }
        }
        public static object EvaluateScriptUnsafe(this IFrame Frame, string Script)
        {
            if (Program.Debug)
                Program.Writer?.WriteLine("EVAL At: {0}\r\nScript: {1}", Frame.Url, Script);

            return Frame.EvaluateScriptAsync(Script).GetAwaiter().GetResult().Result;
        }

        public static CloudflareData BypassCloudflare(this string Url)
        {
            using var WebBrowser = new CefSharp.WinForms.ChromiumWebBrowser("about:blank");
            WebBrowser.CreateControl();
            WebBrowser.WaitInitialize();
            return WebBrowser.BypassCloudflare(Url);
        }

        public static CloudflareData BypassCloudflare(this IWebBrowser WebBrowser, string Url)
        {
            var Status = Main.Status;
            Main.Status = Main.Language.BypassingCloudFlare;

            var OriRequestHandler = WebBrowser.RequestHandler;


            var Container = new CookieContainer();

            WebBrowser.RegisterWebRequestHandlerEvents(null, (sender, e) =>
            {
                var Cookies = e.Headers.GetValues("Set-Cookie");

                if (Cookies == null)
                    return;

                foreach (var Cookie in Cookies)
                {
                    Container.SetCookies(e.WebRequest.RequestUri, Cookie);
                }
            }, (sender, e) =>
            {
                e.DefaultHandler = e.WebRequest.Url != Url;
            });

            WebBrowser.Load(Url);

            var Browser = WebBrowser.GetBrowser();
            Browser.WaitForLoad(10);

            while (Browser.IsCloudflareTriggered())
            {
                while (Browser.IsCloudflareTriggered() && !Browser.IsCloudflareAskingCaptcha())
                {
                    ThreadTools.Wait(100, true);
                }

                if (Browser.IsCloudflareAskingCaptcha())
                {
                    int Tries = 3;
                    while (Browser.IsCloudflareTriggered() && Tries > 0)
                    {
                        if (Browser.GetCurrentUrl() != Url)
                            DefaultBrowser.WaitForLoad(Url);

                        if (!Browser.TurnstileIsSolved() && Tries > 1)
                        {
                            Browser.TurnstileSolve();
                        }
                        else if (WebBrowser is CefSharp.WinForms.ChromiumWebBrowser winWebBrowser)
                        {
                            var MaxWait = 60;
                            BrowserPopup popup = new BrowserPopup(WebBrowser, new Rectangle(0, 0, 1280, 720), () =>
                            {
                                try
                                {
                                    if (Browser.IsCloudflareTriggered() && !Browser.TurnstileIsSolved() && MaxWait-- > 0)
                                        return false;
                                }
                                catch
                                {

                                }
                                return true;
                            });

                            popup.ShowDialog();
                            popup.Focus();
                        }

                        ThreadTools.Wait(1000, true);
                        Browser.WaitForLoad(10);
                        Tries--;
                    }
                }
            }

            Browser.WaitForLoad(15);
            var HTML = Browser.GetHTML();
            var BrowserCookies = Browser.GetCookies();

            foreach (var Cookie in BrowserCookies.ToContainer().GetCookies())
                Container.Add(Cookie);


            WebBrowser.RequestHandler = OriRequestHandler;

            //WebBrowser.Load("about:blank");

            Main.Status = Status;

            if (Program.Debug)
                Program.Writer?.WriteLine("CF Bypass Result: {0}\r\nHTML: {1}", Browser.MainFrame.Url, HTML);

            return new CloudflareData()
            {
                Cookies = Container,
                UserAgent = Browser.GetUserAgent(),
                HTML = HTML
            };
        }

        public static HtmlDocument GetDocument(this ChromiumWebBrowser Browser) => Browser.GetBrowser().GetDocument();
        public static string GetHTML(this ChromiumWebBrowser Browser) => Browser.GetBrowser().GetHTML();
        public static bool IsCloudflareTriggered(this ChromiumWebBrowser Browser) => Browser.GetBrowser().IsCloudflareTriggered();

        public static HtmlDocument GetDocument(this IBrowser Browser)
        {
            var Document = new HtmlDocument();
            Document.LoadHtml(Browser.GetHTML());
            return Document;
        }

        public static string GetHTML(this IBrowser Browser) =>
            AsyncContext.Run(async () => {  try { return await Browser?.MainFrame?.GetSourceAsync(); } catch { return null; } });
        public static bool IsCloudflareTriggered(this IBrowser Browser) => Browser.GetHTML().IsCloudflareTriggered();
        public static bool IsCloudflareAskingCaptcha(this IBrowser Browser) => Browser.GetHTML().IsCloudflareAskingCaptcha();

        public static bool IsCloudflareTriggered(this HtmlDocument Document) => Document.ToHTML().IsCloudflareTriggered();
        public static bool IsCloudflareAskingCaptcha(this HtmlDocument Document) => Document.ToHTML().IsCloudflareAskingCaptcha();
        public static bool IsCloudflareTriggered(this string HTML) => HTML.Contains("Please Wait... | Cloudflare") || HTML.Contains("Attention Required! | Cloudflare") || HTML.Contains("5 seconds...") || HTML.Contains("Checking your browser") || HTML.Contains("DDOS-GUARD") || HTML.Contains("Checking if the site connection is secure") || HTML.Contains("Just a moment...");
        public static bool IsCloudflareAskingCaptcha(this string HTML) => HTML.Contains("why_captcha_headline") || HTML.Contains("captcha-prompt spacer") || HTML.Contains("turnstile-wrapper") || HTML.Contains("Verify you are human by completing the action below");
        public static IFrame GetFrameByUrl(this ChromiumWebBrowser Browser, string UrlFragment) => Browser.GetBrowser().GetFrameByUrl(UrlFragment);
        public static IFrame GetFrameByUrl(this IBrowser Browser, string UrlFragment)
        {
            foreach (var ID in Browser.GetFrameIdentifiers())
            {
                var Frame = Browser.GetFrameByIdentifier(ID);
                if (Frame == null)
                    continue;
                if (Frame.Url.Contains(UrlFragment))
                    return Frame;
            }
            return null;
        }
        public static async Task SetInputFile(this ChromiumWebBrowser Browser, string ElementID, string InputFile) => await Browser.GetBrowser().SetInputFile(ElementID, InputFile);
        public static async Task SetInputFile(this IBrowser Browser, string ElementID,  string InputFile)
        {
            try
            {
                using (var Client = Browser.GetDevToolsClient())
                {
                    var Doc = await Client.DOM.GetDocumentAsync();
                    var Input = await Client.DOM.QuerySelectorAsync(Doc.Root.NodeId, $"#{ElementID}");

                    if (Input.NodeId == 0)
                        throw new NodeNotFoundException();

                    await Client.DOM.SetFileInputFilesAsync(new[] { InputFile }, Input.NodeId);
                }
            }
            catch { }
        }

        public static CloudflareData BypassCloudflare(this ChromiumWebBrowser Browser)
        {
            var URL = Browser.GetCurrentUrl();
            var Bypass = BypassCloudflare(URL);
            foreach (var Cookie in Bypass.Cookies.GetCookies())
            {
                Browser.UpdateCookie(Cookie);
            }
            Browser.WaitForLoad(URL);
            return Bypass;
        }
    }
}
