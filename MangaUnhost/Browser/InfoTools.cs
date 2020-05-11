using CefSharp;
using CefSharp.OffScreen;
using MangaUnhost.Others;
using System.Threading.Tasks;

namespace MangaUnhost.Browser {
    public static class InfoTools {
        public static bool IsLoading(this IBrowser Browser) => Browser.MainFrame.IsLoading();
        public static bool IsLoading(this IFrame Frame)
        {
            if (Frame.Browser.IsLoading)
                return true;
            var Status = (string)Frame.EvaluateScriptAsync(Properties.Resources.GetDocumentStatus).GetAwaiter().GetResult().Result;
            if (Status?.Trim().ToLower() == "complete")
                return false;
            return true;
        }

        public static void WaitForLoad(this ChromiumWebBrowser Browser, string Url)
        {
            Browser.WaitInitialize();
            Browser.Load(Url);
            Browser.GetBrowser().WaitForLoad();
        }

        public static void WaitForLoad(this ChromiumWebBrowser Browser) {
            Browser.WaitInitialize();
            Browser.GetBrowser().WaitForLoad();
        }
        public static void WaitInitialize(this ChromiumWebBrowser Browser) {
            while (!Browser.IsBrowserInitialized)
                ThreadTools.Wait(5, true);
        }

        public static void WaitForLoad(this IBrowser Browser) {
            ThreadTools.Wait(100);
            while (Browser.IsLoading())
                ThreadTools.Wait(5, true);
        }

        public static void WaitForLoad(this IFrame Frame)
        {
            ThreadTools.Wait(100);
            while (Frame.IsLoading())
                ThreadTools.Wait(5, true);
        }

        public static string GetCurrentUrl(this ChromiumWebBrowser Browser) => Browser.GetBrowser().GetCurrentUrl();
        public static string GetCurrentUrl(this IBrowser Browser) => Browser.MainFrame.Url;

        public static string GetUserAgent(this ChromiumWebBrowser Browser) => Browser.GetBrowser().GetUserAgent();

        public static string GetUserAgent(this IBrowser Browser)
        {
            return (string)Browser.MainFrame.EvaluateScriptAsync(Properties.Resources.GetUserAgent).GetAwaiter().GetResult().Result;
        }

        delegate object Invoker();
    }
}
