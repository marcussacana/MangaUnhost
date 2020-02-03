using CefSharp;
using CefSharp.OffScreen;
using HtmlAgilityPack;
using MangaUnhost.Others;
using Nito.AsyncEx;

namespace MangaUnhost.Browser {
    public static class JSTools {

        static ChromiumWebBrowser _DefBrowser = null;
        public static ChromiumWebBrowser DefaultBrowser {
            get {
                if (_DefBrowser != null)
                    return _DefBrowser;

                _DefBrowser = new ChromiumWebBrowser("about:blank");
                _DefBrowser.HookReCaptcha();

                while (!DefaultBrowser.IsBrowserInitialized) {
                    ThreadTools.Wait(50, true);
                }

                return _DefBrowser;
            }
        }

        public static object EvaulateScript(string Script) {
            DefaultBrowser.GetBrowser().WaitForLoad();

            return DefaultBrowser.GetBrowser().MainFrame.EvaluateScriptAsync(Script).GetAwaiter().GetResult().Result;
        }

        public static object EvaluateScript(this ChromiumWebBrowser Browser, string Script) => Browser.GetBrowser().EvaluateScript(Script);
        public static object EvaluateScript(this IBrowser Browser, string Script) {
            Browser.WaitForLoad();

            return Browser.MainFrame.EvaluateScriptAsync(Script).GetAwaiter().GetResult().Result;
        }

        public static CloudflareData BypassCloudFlare(string Url) {
            var Status = Main.Status;
            Main.Status = Main.Language.BypassingCloudFlare;

            var Browser = DefaultBrowser.GetBrowser();
            DefaultBrowser.Load(Url);
            Browser.WaitForLoad();

            if (Browser.GetHTML().Contains("why_captcha_headline")) {
                while (Browser.IsCloudflareTriggered()) {
                    DefaultBrowser.TrySolveCaptcha(Main.Solver);
                    EvaulateScript(Properties.Resources.CloudFlareSubmitCaptcha);
                    ThreadTools.Wait(3000, true);
                    Browser.WaitForLoad();
                }
            } else {
                while (Browser.IsCloudflareTriggered()) {
                    ThreadTools.Wait(100, true);
                    Browser.WaitForLoad();
                }
            }

            Browser.WaitForLoad();
            var HTML = Browser.GetHTML();
            var Cookies = Browser.GetCookies().ToContainer();

            DefaultBrowser.Load("about:blank");
       
            Main.Status = Status;

            return new CloudflareData() {
                Cookies = Cookies,
                UserAgent = Browser.GetUserAgent(),
                HTML = HTML
            };
        }

        public static HtmlDocument GetDocument(this ChromiumWebBrowser Browser) => Browser.GetBrowser().GetDocument();
        public static string GetHTML(this ChromiumWebBrowser Browser) => Browser.GetBrowser().GetHTML();
        public static bool IsCloudflareTriggered(this ChromiumWebBrowser Browser) => Browser.GetBrowser().IsCloudflareTriggered();

        public static HtmlDocument GetDocument(this IBrowser Browser) {
            var Document = new HtmlDocument();
            Document.LoadHtml(Browser.GetHTML());
            return Document;
        }

        public static string GetHTML(this IBrowser Browser) =>
            AsyncContext.Run(async () => await Browser.MainFrame.GetSourceAsync());
        public static bool IsCloudflareTriggered(this IBrowser Browser) => Browser.GetHTML().IsCloudflareTriggered();
        public static bool IsCloudflareTriggered(this string HTML) => HTML.Contains("5 seconds...") || HTML.Contains("Checking your browser") || HTML.Contains("why_captcha_headline") || HTML.Contains("DDOS-GUARD");


    }
}
