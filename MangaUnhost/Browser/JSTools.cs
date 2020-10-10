using CefSharp;
using CefSharp.OffScreen;
using HtmlAgilityPack;
using MangaUnhost.Others;
using Nito.AsyncEx;

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

        public static T EvaluateScript<T>(this ChromiumWebBrowser Browser, string Script) => (T)Browser.GetBrowser().EvaluateScript(Script);
        public static T EvaluateScript<T>(this IBrowser Browser, string Script) => (T)Browser.MainFrame.EvaluateScript(Script);
        public static void EvaluateScript<T>(this ChromiumWebBrowser Browser, string Script, out T Result) => Result = (T)Browser.GetBrowser().EvaluateScript(Script);
        public static void EvaluateScript<T>(this IBrowser Browser, string Script, out T Result) => Result = (T)Browser.MainFrame.EvaluateScript(Script);
        public static object EvaluateScript(this ChromiumWebBrowser Browser, string Script) => Browser.GetBrowser().EvaluateScript(Script);
        public static object EvaluateScript(this IBrowser Browser, string Script)
        {
            return Browser.MainFrame.EvaluateScript(Script);
        }
        public static T EvaluateScript<T>(this IFrame Frame, string Script) => (T)Frame.EvaluateScript(Script);
        public static object EvaluateScript(this IFrame Frame, string Script)
        {
            Frame.WaitForLoad();

            return Frame.EvaluateScriptAsync(Script).GetAwaiter().GetResult().Result;
        }

        public static CloudflareData BypassCloudflare(string Url)
        {
            var Status = Main.Status;
            Main.Status = Main.Language.BypassingCloudFlare;

            var Browser = DefaultBrowser.GetBrowser();
            DefaultBrowser.Load(Url);
            Browser.WaitForLoad();

            while (Browser.IsCloudflareTriggered())
            {
                ThreadTools.Wait(100, true);
                Browser.WaitForLoad();
            }

            if (Browser.GetHTML().Contains("why_captcha_headline"))
            {
                while (Browser.IsCloudflareTriggered() || Browser.IsCloudflareAskingCaptcha())
                {
                    if (!DefaultBrowser.ReCaptchaIsSolved())
                    {
                        DefaultBrowser.ReCaptchaTrySolve(Main.Solver);
                        EvaluateScript(Properties.Resources.CloudFlareSubmitCaptcha);
                    }
                    if (!DefaultBrowser.hCaptchaIsSolved())
                    {
                        DefaultBrowser.hCaptchaSolve();
                        ThreadTools.Wait(1000, true);
                    }
                    ThreadTools.Wait(1000, true);
                    Browser.WaitForLoad();
                }
            }

            Browser.WaitForLoad();
            var HTML = Browser.GetHTML();
            var Cookies = Browser.GetCookies().ToContainer();

            DefaultBrowser.Load("about:blank");

            Main.Status = Status;

            return new CloudflareData()
            {
                Cookies = Cookies,
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
            AsyncContext.Run(async () => await Browser.MainFrame.GetSourceAsync());
        public static bool IsCloudflareTriggered(this IBrowser Browser) => Browser.GetHTML().IsCloudflareTriggered();
        public static bool IsCloudflareAskingCaptcha(this IBrowser Browser) => Browser.GetHTML().IsCloudflareAskingCaptcha();

        public static bool IsCloudflareTriggered(this HtmlDocument Document) => Document.ToHTML().IsCloudflareTriggered();
        public static bool IsCloudflareAskingCaptcha(this HtmlDocument Document) => Document.ToHTML().IsCloudflareAskingCaptcha();
        public static bool IsCloudflareTriggered(this string HTML) => HTML.Contains("5 seconds...") || HTML.Contains("Checking your browser") || HTML.Contains("DDOS-GUARD");
        public static bool IsCloudflareAskingCaptcha(this string HTML) => HTML.Contains("why_captcha_headline");
        public static IFrame GetFrameByUrl(this ChromiumWebBrowser Browser, string UrlFragment) => Browser.GetBrowser().GetFrameByUrl(UrlFragment);
        public static IFrame GetFrameByUrl(this IBrowser Browser, string UrlFragment)
        {
            foreach (var ID in Browser.GetFrameIdentifiers())
            {
                var Frame = Browser.GetFrame(ID);
                if (Frame == null)
                    continue;
                if (Frame.Url.Contains(UrlFragment))
                    return Frame;
            }
            return null;
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
